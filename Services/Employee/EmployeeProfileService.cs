using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.ViewModels;

public class EmployeeProfileService : IEmployeeProfileService
{
   private readonly ApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;
    private readonly ICloudinaryService _cloudinaryService;
    
    public EmployeeProfileService(ApplicationDbContext db, IAuthService auth, IWebHostEnvironment env, ICloudinaryService cloudinaryService)
    {
        _db = db; _auth = auth; _env = env; _cloudinaryService = cloudinaryService;
    }

    public Employee? GetById(string employeeId)
        => _db.Employees.FirstOrDefault(e => e.EmployeeID == employeeId);

    public bool EditProfile(string employeeId, EditEmployeeProfile m, out string error)
    {
        error = string.Empty;
        var e = _db.Employees.FirstOrDefault(x => x.EmployeeID == employeeId);
        if (e == null) { error = "Nhân viên không tồn tại."; return false; }

        // Validate trùng SĐT/Email (khác bản thân)
        if (!string.IsNullOrWhiteSpace(m.PhoneNumber) &&
            _db.Employees.Any(x => x.PhoneNumber == m.PhoneNumber && x.EmployeeID != employeeId))
        { error = "Số điện thoại đã tồn tại."; return false; }

        if (!string.IsNullOrWhiteSpace(m.Email) &&
            _db.Employees.Any(x => x.Email == m.Email && x.EmployeeID != employeeId))
        { error = "Email đã tồn tại."; return false; }

        // Đổi mật khẩu (nếu có yêu cầu)
        if (!string.IsNullOrEmpty(m.NewPassword))
        {
            if (string.IsNullOrEmpty(m.ConfirmNewPassword) || m.NewPassword != m.ConfirmNewPassword)
            {
                error = "Xác nhận mật khẩu không khớp.";
                return false;
            }

            var ok = e.IsHashed
            ? _auth.HashPassword(m.CurrentPassword ?? "") == e.Password
            : (m.CurrentPassword ?? "") == e.Password;

            if (!ok) { error = "Mật khẩu hiện tại không đúng."; return false; }

            e.Password = _auth.HashPassword(m.NewPassword);
            e.IsHashed = true;
        }


        // Cập nhật trường hồ sơ
        e.DateOfBirth = m.DateOfBirth;
        e.Nationality = m.Nationality;
        e.Gender = m.Gender;
        e.Ethnicity = m.Ethnicity;
        e.PhoneNumber = m.PhoneNumber;
        e.Email = m.Email;
        e.EmergencyPhone1 = m.EmergencyPhone1;
        e.EmergencyPhone2 = m.EmergencyPhone2;

        _db.SaveChanges();
        return true;
    }

    public async Task<bool> UploadAvatar(string employeeId, IFormFile avatar)
    {
        if (avatar == null || avatar.Length == 0) return false;

        var e = _db.Employees.FirstOrDefault(x => x.EmployeeID == employeeId);
        if (e == null) return false;

        // Upload lên Cloudinary
        var cloudinaryUrl = await _cloudinaryService.UploadImageAsync(avatar, "uploads/avatars");
        
        if (string.IsNullOrEmpty(cloudinaryUrl))
            return false;

        // Xóa avatar cũ trên Cloudinary nếu có (nếu là URL Cloudinary)
        // Note: Có thể cải thiện thêm bằng cách lưu public_id để xóa chính xác hơn
        // Hiện tại chỉ update URL mới, không xóa ảnh cũ để tránh lỗi

        // Update DB với URL từ Cloudinary
        e.AvatarUrl = cloudinaryUrl;
        _db.SaveChanges();
        return true;
    }
    
    
   
}
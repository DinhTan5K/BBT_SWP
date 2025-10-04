using start.Data;
using start.Models;
public class ProfileService : IProfileService
{

    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;

    public ProfileService(ApplicationDbContext context, IAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public bool EditProfile(int userId, EditProfileModel model, out string error)
    {
        error = string.Empty;

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId);
        if (user == null)
        {
            error = "User không tồn tại.";
            return false;
        }

        // Check phone trùng với user khác
        if (_context.Customers.Any(c => c.Phone == model.Phone && c.CustomerID != userId))
        {
            error = "Số điện thoại đã tồn tại.";
            return false;
        }

        // Nếu đổi password
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (!_authService.VerifyPassword(model.CurrentPassword ?? "", user.Password!))
            {
                error = "Mật khẩu hiện tại không đúng.";
                return false;
            }
            user.Password = _authService.HashPassword(model.NewPassword);
        }

        // Update các thông tin khác
        user.Name = model.Name;
        user.Phone = model.Phone;
        user.Address = model.Address;
        user.BirthDate = model.BirthDate;

        _context.SaveChanges();
        return true;
    }


    public async Task<bool> UploadAvatar(int userId, IFormFile avatar)
    {
        if (avatar == null || avatar.Length == 0)
            return false;

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId);
        if (user == null)
            return false;

        // Xóa avatar cũ nếu có và không phải default
        if (!string.IsNullOrEmpty(user.ProfileImagePath) && !user.ProfileImagePath.Contains("/img/"))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfileImagePath.TrimStart('/'));
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        // Lưu file mới
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(avatar.FileName)}";
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        // Update DB
        user.ProfileImagePath = $"/uploads/avatars/{fileName}";
        _context.SaveChanges();

        return true;
    }

    
    public Customer? GetUserById(int userId)
    {
        return _context.Customers.FirstOrDefault(c => c.CustomerID == userId);
    }
}

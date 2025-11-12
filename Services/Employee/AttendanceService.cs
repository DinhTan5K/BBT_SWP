using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using start.Data;
using start.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
namespace start.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;

        public AttendanceService(ApplicationDbContext db, IWebHostEnvironment env, IConfiguration cfg)
        {
            _db = db;
            _env = env;
            _cfg = cfg; 
        }

        public async Task<(bool success, string message, Attendance? attendance)> CheckInAsync(
    string employeeId, int? workScheduleId, string capturedImageBase64)
{
    try
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
        if (employee == null) return (false, "Không tìm thấy nhân viên.", null);
        if (string.IsNullOrEmpty(employee.AvatarUrl))
            return (false, "Nhân viên chưa có ảnh khuôn mặt đăng ký.", null);

        var today = DateTime.Today;
        var workSchedule = await _db.WorkSchedules
            .FirstOrDefaultAsync(w => w.EmployeeID == employeeId && w.Date == today);

        if (workSchedule == null)
            return (false, $"Hôm nay ({today:dd/MM/yyyy}) bạn không có ca làm việc.", null);

        if (!ShiftTimeHelper.CanCheckIn(DateTime.Now, workSchedule.Date, workSchedule.Shift, out var msg))
            return (false, msg, null);

        var already = await _db.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeID == employeeId && a.CheckInTime.Date == today && a.CheckOutTime == null);
        if (already != null)
            return (false, "Bạn đã check-in hôm nay rồi.", already);

        var faceMatch = await CompareFacesAsync(employee, capturedImageBase64);
        if (!faceMatch)
            return (false, "Xác thực khuôn mặt thất bại. Vui lòng thử lại.", null);

        var checkInImageUrl = await SaveCapturedImageAsync(capturedImageBase64, $"checkin_{employeeId}_{DateTime.Now:yyyyMMddHHmmss}.jpg");

        var attendance = new Attendance
        {
            EmployeeID = employeeId,
            WorkScheduleID = workSchedule.WorkScheduleID,
            CheckInTime = DateTime.Now,
            CheckInImageUrl = checkInImageUrl,
            IsFaceVerified = true,
            CreatedAt = DateTime.Now
        };

        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(); // ✅ Giữ nguyên SaveChanges như cũ (ổn định)

        // Chỉ dòng này để tránh lỗi trigger
        _db.ChangeTracker.Clear();
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE WorkSchedules SET IsActive = 1 WHERE WorkScheduleID = {0}", workSchedule.WorkScheduleID);

        return (true, "Check-in thành công!", attendance);
    }
    catch (Exception ex)
    {
        return (false, $"Lỗi: {ex.Message}", null);
    }
}
public async Task<(bool success, string message, Attendance? attendance)> CheckOutAsync(
    string employeeId, string capturedImageBase64)
{
    try
    {
        var today = DateTime.Today;
        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(a =>
                a.EmployeeID == employeeId &&
                a.CheckInTime.Date == today &&
                a.CheckOutTime == null);

        if (attendance == null)
            return (false, "Bạn chưa check-in hôm nay.", null);

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
        if (employee == null || string.IsNullOrEmpty(employee.AvatarUrl))
            return (false, "Không tìm thấy nhân viên hoặc chưa đăng ký ảnh khuôn mặt.", null);

        var faceMatch = await CompareFacesAsync(employee, capturedImageBase64);
        if (!faceMatch)
            return (false, "Xác thực khuôn mặt thất bại. Vui lòng thử lại.", null);

        var checkOutImageUrl = await SaveCapturedImageAsync(capturedImageBase64, $"checkout_{employeeId}_{DateTime.Now:yyyyMMddHHmmss}.jpg");

        attendance.CheckOutTime = DateTime.Now;
        attendance.CheckOutImageUrl = checkOutImageUrl;

        await _db.SaveChangesAsync(); // ✅ Giữ lại như trước (để view cập nhật TempData)

        return (true, "Check-out thành công!", attendance);
    }
    catch (Exception ex)
    {
        return (false, $"Lỗi: {ex.Message}", null);
    }
}


        public async Task<Attendance?> GetTodayCheckInAsync(string employeeId)
        {
            var today = DateTime.Today;
            return await _db.Attendances
                .Include(a => a.WorkSchedule)
                .FirstOrDefaultAsync(a => 
                    a.EmployeeID == employeeId && 
                    a.CheckInTime.Date == today &&
                    a.CheckOutTime == null);
        }

        public async Task<List<Attendance>> GetAttendanceHistoryAsync(string employeeId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.Attendances
                .Include(a => a.WorkSchedule)
                .Where(a => a.EmployeeID == employeeId);

            if (fromDate.HasValue)
                query = query.Where(a => a.CheckInTime.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.CheckInTime.Date <= toDate.Value.Date);

            return await query
                .OrderByDescending(a => a.CheckInTime)
                .ToListAsync();
        }

        public async Task<bool> UploadFaceImageAsync(string employeeId, IFormFile faceImage)
        {
            try
            {
                var employee = await _db.Employees.FindAsync(employeeId);
                if (employee == null)
                    return false;

                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "faces");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"face_{employeeId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(faceImage.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await faceImage.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/faces/{fileName}";
                employee.AvatarUrl = imageUrl;
                await _db.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CompareFacesAsync(Employee employee, string capturedImageBase64)

{
    try
    {
       var apiKey = _cfg["FaceApi:ApiKey"];
var apiSecret = _cfg["FaceApi:ApiSecret"];
var apiUrl = _cfg["FaceApi:Url"];


       if (employee == null || string.IsNullOrEmpty(employee.AvatarUrl))
    return false;

var originalPath = Path.Combine(_env.WebRootPath, employee.AvatarUrl.TrimStart('/'));
if (!System.IO.File.Exists(originalPath))
{
    Console.WriteLine("❌ Không tìm thấy ảnh Avatar: " + originalPath);
    return false;
}


        var originalBytes = await System.IO.File.ReadAllBytesAsync(originalPath);
        string originalBase64 = Convert.ToBase64String(originalBytes);

        using var http = new HttpClient();

var boundary = "----FaceppBoundary" + DateTime.Now.Ticks.ToString("x");
using var form = new MultipartFormDataContent(boundary);

// ✅ Tạo phần tử form với header chuẩn "form-data"
var keyContent = new StringContent(apiKey, Encoding.UTF8);
keyContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
{
    Name = "\"api_key\""
};
form.Add(keyContent);

var secretContent = new StringContent(apiSecret, Encoding.UTF8);
secretContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
{
    Name = "\"api_secret\""
};
form.Add(secretContent);

var img1Content = new StringContent(originalBase64, Encoding.UTF8);
img1Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
{
    Name = "\"image_base64_1\""
};
form.Add(img1Content);

var img2Content = new StringContent(capturedImageBase64, Encoding.UTF8);
img2Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
{
    Name = "\"image_base64_2\""
};
form.Add(img2Content);

// ✅ Đảm bảo Content-Type có boundary
form.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/form-data")
{
    Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary) }
};

// ✅ Gửi và ghi log
Console.WriteLine($"[Face++] Sending request with boundary={boundary}");
var response = await http.PostAsync(apiUrl, form);
var json = await response.Content.ReadAsStringAsync();
Console.WriteLine("Response JSON: " + json);

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("error_message", out var err))
        {
            Console.WriteLine("Face++ API Error: " + err.GetString());
            return false;
        }

        if (!doc.RootElement.TryGetProperty("confidence", out var confElem))
            return false;

        double confidence = confElem.GetDouble();
        Console.WriteLine($"Face++ confidence = {confidence}");

        // Ghi log ra file để kiểm tra
        var logPath = Path.Combine(_env.ContentRootPath, "face_log.txt");
        await File.AppendAllTextAsync(logPath, $"[{DateTime.Now}] Confidence={confidence}\n");

        if (confidence < 40)
        {
            Console.WriteLine("❌ Quá thấp, ảnh khác người.");
            return false;
        }

        if (confidence >= 55)
        {
            Console.WriteLine("✅ Ảnh giống ở mức chấp nhận được (test mode).");
            return true;
        }

        Console.WriteLine("⚠️ Ảnh gần giống nhưng chưa đạt ngưỡng. Hãy thử chụp lại dưới ánh sáng tốt hơn.");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Face++ exception: " + ex.Message);
        return false;
    }
}


        private async Task<string> SaveCapturedImageAsync(string base64Image, string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "attendance");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var base64Data = base64Image.Contains(",")
                    ? base64Image.Split(',')[1]
                    : base64Image;
                var imageBytes = Convert.FromBase64String(base64Data);

                var filePath = Path.Combine(uploadsPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                return $"/uploads/attendance/{fileName}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}


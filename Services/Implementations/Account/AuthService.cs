using System.Security.Cryptography;
using System.Text;
using start.Data;
using start.Models;
using start.Services;

public class AuthService : IAuthService
{

    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public AuthService(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }



    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }

    public Customer? LoginCustomer(string loginId, string password)
    {
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            return null;

        var input = loginId.Trim().ToLowerInvariant();
        var hashedPassword = HashPassword(password);

        var user = _context.Customers

            .FirstOrDefault(c =>
                (
                    (c.Email != null && c.Email.ToLower() == input) ||
                    (c.Username != null && c.Username == loginId) // giữ nguyên so sánh username như hiện tại
                )
                && c.Password == hashedPassword
            );

        if (user == null) return null;

        if (!user.IsEmailConfirmed)
            throw new Exception("Email chưa xác thực");

        return user;
    }

    public Employee? LoginEmployee(string loginId, string password)
    {
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            return null;

        var input = loginId.Trim().ToLowerInvariant();

        var emp = _context.Employees

            .FirstOrDefault(e =>
                (e.Email != null && e.Email.ToLower() == input) ||
                (e.EmployeeID != null && e.EmployeeID.ToLower() == input)
            );

        if (emp == null) return null;

        // So khớp mật khẩu (giữ nguyên cơ chế hiện tại)
        bool ok = emp.IsHashed
            ? emp.Password == HashPassword(password)
            : emp.Password == password;

        return ok ? emp : null;
    }

    public Customer HandleGoogleLogin(string email, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email từ Google không hợp lệ.");

        var lowerEmail = email.ToLower();
        var user = _context.Customers
            .FirstOrDefault(u => u.Email.ToLower() == lowerEmail);

        if (user == null)
        {
            user = new Customer
            {
                Name = name ?? "Unknown",
                Email = email,
                Username = email.Split('@')[0],
                Password = null,   // Google login không cần password
                Phone = null,
                CreatedAt = DateTime.Now,
                IsEmailConfirmed = true,
            };

            _context.Customers.Add(user);
            _context.SaveChanges();
        }

        return user;
    }


    public Customer? Register(Customer model, out string otp, out Dictionary<string, string> errors)
    {
        otp = string.Empty;
        errors = new Dictionary<string, string>();

        model.Email = model.Email?.Trim();
        model.Username = model.Username?.Trim();
        model.Phone = model.Phone?.Trim();

        if (string.IsNullOrWhiteSpace(model.Email))
            errors["Email"] = "Email không được để trống.";

        if (string.IsNullOrWhiteSpace(model.Password))
            errors["Password"] = "Mật khẩu không được để trống.";

        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length <= 6)
            errors["Password"] = "Mật khẩu phải dài hơn 6 ký tự.";

        if (_context.Customers.Any(c => c.Email.ToLower() == model.Email.ToLower()))
            errors["Email"] = "Email đã tồn tại.";

        if (_context.Customers.Any(c => c.Phone == model.Phone))
            errors["Phone"] = "Số điện thoại đã tồn tại.";

        if (!string.IsNullOrWhiteSpace(model.Username) &&
            _context.Customers.Any(c => c.Username == model.Username))
            errors["Username"] = "Username đã tồn tại.";

        if (errors.Count > 0)
            return null;

        // Hash password
        model.Password = HashPassword(model.Password);

        model.IsEmailConfirmed = false;

        // Sinh OTP
        otp = new Random().Next(100000, 999999).ToString();
        model.OtpCode = otp;
        model.OtpExpired = DateTime.Now.AddMinutes(10);

        _context.Customers.Add(model);
        _context.SaveChanges();

        return model;
    }




    public bool ChangePassword(int userId, string currentPassword, string newPassword, out string error)
    {
        error = string.Empty;

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId);
        if (user == null)
        {
            error = "User không tồn tại.";
            return false;
        }

        if (!VerifyPassword(currentPassword, user.Password))
        {
            error = "Mật khẩu hiện tại không đúng.";
            return false;
        }

        // Enforce password length > 6 characters (i.e., at least 7)
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length <= 6)
        {
            error = "Mật khẩu mới phải dài hơn 6 ký tự.";
            return false;
        }

        user.Password = HashPassword(newPassword);
        _context.SaveChanges();

        return true;
    }


    public bool ResetPassword(string email, string newPassword, out string error)
    {
        error = string.Empty;

        var user = _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.Trim().ToLower());
        if (user == null)
        {
            error = "Email không tồn tại.";
            return false;
        }

        // Enforce password length > 6 characters (i.e., at least 7)
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length <= 6)
        {
            error = "Mật khẩu mới phải dài hơn 6 ký tự.";
            return false;
        }

        user.Password = HashPassword(newPassword);

        user.OtpCode = null;
        user.OtpExpired = null;

        _context.SaveChanges();
        return true;
    }

    public bool SetPassword(int userId, string newPassword, out string error)
    {
        error = string.Empty;

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId);
        if (user == null)
        {
            error = "User không tồn tại.";
            return false;
        }

        if (!string.IsNullOrEmpty(user.Password))
        {
            error = "Tài khoản đã có mật khẩu. Vui lòng dùng Đổi mật khẩu.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length <= 6)
        {
            error = "Mật khẩu mới phải dài hơn 6 ký tự.";
            return false;
        }

        user.Password = HashPassword(newPassword);
        _context.SaveChanges();
        return true;
    }

    public bool VerifyEmail(string email, string otp)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            throw new Exception("Email và OTP là bắt buộc.");

        var user = _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.Trim().ToLower());

        if (user == null)
            throw new Exception("Không tìm thấy user.");

        // Kiểm tra OTP + thời hạn
        if (user.OtpCode != otp)
            throw new Exception("OTP không đúng.");

        if (user.OtpExpired == null || user.OtpExpired < DateTime.Now)
            throw new Exception("OTP đã hết hạn.");

        // Xác thực email thành công
        user.IsEmailConfirmed = true;
        user.OtpCode = null;
        user.OtpExpired = null;

        _context.SaveChanges();

        return true;
    }


    public bool SendOtp(string email, string purpose, out string otp, out string error)
    {
        otp = string.Empty;
        error = string.Empty;

        var user = _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.ToLower());
        if (user == null)
        {
            error = "Email không tồn tại trong hệ thống.";
            return false;
        }

        otp = new Random().Next(100000, 999999).ToString();

        if (purpose == "reset")
        {
            user.OtpCode = otp;
            user.OtpExpired = DateTime.Now.AddMinutes(10);
            _context.SaveChanges();
        }
        else
        {
            // purpose khác có thể dùng để lưu session hoặc cache
            user.OtpCode = otp;
            user.OtpExpired = DateTime.Now.AddMinutes(10);
            _context.SaveChanges();
        }

        try
        {
            _emailService.SendOtp(email, otp); // gọi service gửi email
            return true;
        }
        catch (Exception ex)
        {
            error = "Lỗi khi gửi email: " + ex.Message;
            return false;
        }
    }


}

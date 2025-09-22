using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using start.Services;
using Microsoft.AspNetCore.Hosting;
using System.IO;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApplicationDbContext context, EmailService emailService, IWebHostEnvironment env)
    {
        _context = context;
        _emailService = emailService;
        _env = env;
    }

    [HttpGet("login-google")]
    public IActionResult LoginGoogle()
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("LoginGoogleCallback", "Account")
        };
        return Challenge(props, "Google");
    }

    [HttpGet("login-google-callback")]
    public async Task<IActionResult> LoginGoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return RedirectToAction("Login");
        }

        var claims = result.Principal.Identities.First().Claims;
        var email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

        var user = _context.Customers.FirstOrDefault(c => c.Email == email);
        if (user == null)
        {
            user = new Customer
            {
                Email = email,
                Name = name,
                IsEmailConfirmed = true
            };
            _context.Customers.Add(user);
            _context.SaveChanges();
        }

        // ✅ Lưu Session sau khi login Google
        HttpContext.Session.SetInt32("CustomerID", user.CustomerID);
        HttpContext.Session.SetString("CustomerName", user.Name ?? "");
        HttpContext.Session.SetString("ProfileImagePath", user.ProfileImagePath ?? "");

        return RedirectToAction("Privacy", "Home");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(Customer model)
    {
        if (ModelState.IsValid)
        {
            model.Password = HashPassword(model.Password ?? "");
            model.IsEmailConfirmed = false;

            // tạo OTP
            var otp = new Random().Next(100000, 999999).ToString();
            model.OtpCode = otp;

            _context.Customers.Add(model);
            _context.SaveChanges();

            // gửi mail
            _emailService.SendOtp(model.Email ?? "", otp);

            // chuyển sang verify email
            return RedirectToAction("VerifyEmail", new { email = model.Email });
        }

        return View(model);
    }

    [HttpPost]
    public IActionResult SendOtp(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Json(new { success = false, message = "Email không được để trống" });
        }

        var otp = new Random().Next(100000, 999999).ToString();

        HttpContext.Session.SetString("OTP", otp);
        HttpContext.Session.SetString("EmailForOtp", email);

        try
        {
            _emailService.SendOtp(email, otp);
            return Json(new { success = true, message = "OTP đã được gửi tới email" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi gửi email: " + ex.Message });
        }
    }

    [HttpGet]
    public IActionResult VerifyEmail(string email)
    {
        return View(new VerifyEmailViewModel { Email = email });
    }

    [HttpPost]
    public IActionResult VerifyEmail(VerifyEmailViewModel model)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);

        if (customer != null && customer.OtpCode == model.OtpCode)
        {
            customer.IsEmailConfirmed = true;
            customer.OtpCode = null;
            _context.SaveChanges();

            return RedirectToAction("Login", "Home");
        }

        ModelState.AddModelError("", "Mã OTP không đúng.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Password: {password}");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu");
            return View();
        }

        var hashedPassword = HashPassword(password);

        var user = _context.Customers
            .FirstOrDefault(c => c.Email == email && c.Password == hashedPassword);

        if (user == null)
        {
            ModelState.AddModelError("", "Sai email hoặc mật khẩu");
            return View();
        }

        if (!user.IsEmailConfirmed)
        {
            ModelState.AddModelError("", "Email chưa được xác thực. Vui lòng kiểm tra Gmail.");
            return View();
        }

        HttpContext.Session.SetInt32("CustomerID", user.CustomerID);
        HttpContext.Session.SetString("CustomerName", user.Name ?? "");
        HttpContext.Session.SetString("ProfileImagePath", user.ProfileImagePath ?? "");

        return RedirectToAction("Privacy", "Home");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ChangePassword(ChangePassword model)
    {
        int? userId = HttpContext.Session.GetInt32("CustomerID");
        if (userId == null)
            return RedirectToAction("Login");

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId.Value);
        if (user == null)
            return RedirectToAction("Login");

        var hashedCurrent = HashPassword(model.CurrentPassword ?? "");
        if (user.Password != hashedCurrent)
        {
            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        if (model.NewPassword != model.ConfirmNewPassword)
        {
            ModelState.AddModelError("", "Mật khẩu mới không trùng nhau.");
            return View(model);
        }

        user.Password = HashPassword(model.NewPassword);
        _context.SaveChanges();

        ViewBag.Message = "✅ Đổi mật khẩu thành công!";
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public IActionResult SendOtpForReset(string email)
    {
        var user = _context.Customers.FirstOrDefault(c => c.Email == email);
        if (user == null)
            return Json(new { success = false, message = "Email không tồn tại" });

        string otp = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("ResetOtp", otp);
        HttpContext.Session.SetString("ResetEmail", email);

        _emailService.SendOtp(email, otp);

        return RedirectToAction("ResetPassword","Account");
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        return View(new ResetPasswordViewModel());
    }

    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        var sessionOtp = HttpContext.Session.GetString("ResetOtp");
        var sessionEmail = HttpContext.Session.GetString("ResetEmail");

        if (model.OtpCode != sessionOtp)
        {
            ModelState.AddModelError("", "OTP không đúng");
            return View(model);
        }

        var user = _context.Customers.FirstOrDefault(c => c.Email == sessionEmail);
        if (user != null)
        {
            user.Password = HashPassword(model.NewPassword);
            _context.SaveChanges();
        }

        HttpContext.Session.Remove("ResetOtp");
        HttpContext.Session.Remove("ResetEmail");

        ViewBag.Message = "✅ Đổi mật khẩu thành công!";
        return View();
    }

    [HttpGet]
    public IActionResult EditProfile()
    {
        int? userId = HttpContext.Session.GetInt32("CustomerID");
        if (userId == null) return RedirectToAction("Privacy", "Home");

        var user = _context.Customers.FirstOrDefault(c => c.CustomerID == userId.Value);
        if (user == null) return RedirectToAction("Privacy", "Home");

        var vm = new EditProfile
        {
            Name = user.Name,
            Phone = user.Phone,
            Address = user.Address,
            BirthDate = user.BirthDate,
            CurrentProfileImagePath = user.ProfileImagePath
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult EditProfile(EditProfile model)
    {
        int? userId = HttpContext.Session.GetInt32("CustomerID");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = _context.Customers.FirstOrDefault(c => c.CustomerID == userId.Value);
        if (customer == null) return NotFound();

        customer.Name = model.Name;
        customer.Phone = model.Phone;
        customer.Address = model.Address;
        customer.BirthDate = model.BirthDate;

        if (model.ProfileImage != null)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "images/profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                model.ProfileImage.CopyTo(fileStream);
            }

            customer.ProfileImagePath = "/images/profiles/" + uniqueFileName;
        }

        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới không khớp.");
                return View(model);
            }
            customer.Password = HashPassword(model.NewPassword);
        }

        _context.SaveChanges();

        HttpContext.Session.SetString("ProfileImagePath", customer.ProfileImagePath ?? "");

        return RedirectToAction("Privacy", "Home");
    }
}
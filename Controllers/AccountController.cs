using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using start.Services;



public class AccountController : Controller
{

    //ROOT
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;


    public AccountController(ApplicationDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    //FUNCTION
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

            return RedirectToAction("Login", "Account");
        }

        ModelState.AddModelError("", "Mã OTP không đúng.");
        return View(model);
    }





    [HttpPost]
    public IActionResult SendOtp(string email, string purpose = "general")
    {

        if (string.IsNullOrEmpty(email))
            return Json(new { success = false, message = "Email không được để trống" });

        if (purpose == "reset")
        {
            var user = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (user == null)
           ViewBag.ErrorMessage = "Email không tồn tại trong hệ thống";
            return View("ForgotPassword");
        }

        string otp = new Random().Next(100000, 999999).ToString();


        if (purpose == "reset")
        {
            HttpContext.Session.SetString("ResetOtp", otp);
            HttpContext.Session.SetString("ResetEmail", email);
        }
        else
        {
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("EmailForOtp", email);
        }

        try
        {
            _emailService.SendOtp(email, otp);

            if (purpose == "reset")
                return RedirectToAction("ResetPassword", "Account");
            else
                return Json(new { success = true, message = "OTP đã được gửi tới email" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi gửi email: " + ex.Message });
        }
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


    /// ////////////////

    [HttpGet("login-google")]
    public IActionResult LoginGoogle()
    {
        var props = new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") };
        return Challenge(props, "Google");
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    //Register

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


            var otp = new Random().Next(100000, 999999).ToString();
            model.OtpCode = otp;

            _context.Customers.Add(model);
            _context.SaveChanges();

            _emailService.SendOtp(model.Email ?? "", otp);

            return RedirectToAction("VerifyEmail", new { email = model.Email });
        }

        return View(model);
    }


    //Login

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

        return RedirectToAction("Index", "Home");
    }


}

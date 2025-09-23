using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using start.Services;
using System.Security.Claims;



public class AccountController : Controller
{

    //ROOT
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApplicationDbContext context, EmailService emailService, IWebHostEnvironment env)
    {
        _context = context;
        _emailService = emailService;
        _env = env;
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
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleResponse")
        };
        return Challenge(props, "Google");
    }
    [HttpGet]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded) return RedirectToAction("Login");

        var claims = result.Principal.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        // DEBUG: in ra tất cả claim
        Console.WriteLine("=== Google Claims ===");
        foreach (var c in claims)
        {
            Console.WriteLine($"Type: {c.Type}, Value: {c.Value}");
        }

        // Tìm user trong DB
        var user = _context.Customers.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            user = new Customer
            {
                Name = name ?? "Unknown",
                Email = email ?? "noemail@example.com",
                Username = email != null ? email.Split('@')[0] : $"user{Guid.NewGuid()}",
                Password = null,
                Phone = null,
                CreatedAt = DateTime.Now,
                IsEmailConfirmed = true
            };

            // DEBUG: in thông tin trước khi save
            Console.WriteLine("=== New Customer ===");
            Console.WriteLine($"Name: {user.Name}");
            Console.WriteLine($"Email: {user.Email}");
            Console.WriteLine($"Username: {user.Username}");
            Console.WriteLine($"Phone: {user.Phone}");
            Console.WriteLine($"Password: {(user.Password == null ? "NULL" : user.Password)}");

            _context.Customers.Add(user);
            _context.SaveChanges();
        }
        else
        {
            Console.WriteLine($"User already exists: {user.CustomerID} - {user.Email}");
        }

        HttpContext.Session.SetInt32("CustomerID", user.CustomerID);

        return RedirectToAction("Profile", "Account");
    }




    [HttpGet]
    public async Task<IActionResult> Logout()
    {

        HttpContext.Session.Clear();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
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
        bool isGoogleLogin = false;

        if (!isGoogleLogin && string.IsNullOrEmpty(model.Password))
        {
            ModelState.AddModelError("", "Password không được trống");
        }

        if (_context.Customers.Any(c => c.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email đã được đăng ký.");
        }

        if (_context.Customers.Any(c => c.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Username đã tồn tại.");
        }

        if (_context.Customers.Any(c => c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "Số điện thoại đã tồn tại.");
        }

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
    public IActionResult Login(string loginId, string password)
    {
        Console.WriteLine($"LoginId: {loginId}");
        Console.WriteLine($"Password: {password}");

        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập Email/Username và mật khẩu");
            return View();
        }

        var hashedPassword = HashPassword(password);


        var user = _context.Customers
            .FirstOrDefault(c =>
                (c.Email == loginId || c.Username == loginId) &&
                c.Password == hashedPassword);

        if (user == null)
        {
            ModelState.AddModelError("", "Sai Email/Username hoặc mật khẩu");
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



    [HttpGet]
    public IActionResult Profile()
    {
        Customer? user = null;

        int? userId = HttpContext.Session.GetInt32("CustomerID");
        if (userId != null)
        {
            user = _context.Customers
                .AsNoTracking()
                .FirstOrDefault(c => c.CustomerID == userId.Value);

            if (user != null)
            {
                return View(user);
            }
        }


        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value
                        ?? User.FindFirst("email")?.Value;

            user = _context.Customers
                .AsNoTracking()
                .FirstOrDefault(c => c.Email == email);

            if (user != null)
            {
                return View(user);
            }
        }

        return RedirectToAction("Login", "Account");
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

        return RedirectToAction("Profile", "Account");
    }

[HttpPost]
public async Task<IActionResult> UploadAvatar(IFormFile avatar)
{
    var customerId = HttpContext.Session.GetInt32("CustomerID");
    if (customerId == null) return RedirectToAction("Login");

    if (avatar != null && avatar.Length > 0)
    {
        var customer = _context.Customers.Find(customerId);
        if (customer == null) return NotFound();

        // Xóa file cũ (nếu có và không phải ảnh default)
        if (!string.IsNullOrEmpty(customer.ProfileImagePath) 
            && !customer.ProfileImagePath.Contains("/img/"))
        {
            var oldPath = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "wwwroot", 
                customer.ProfileImagePath.TrimStart('/')
            );
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(avatar.FileName)}";
        var path = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "wwwroot/uploads/avatars", 
            fileName
        );

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await avatar.CopyToAsync(stream);
        }

        var dbPath = $"/uploads/avatars/{fileName}";
        customer.ProfileImagePath = dbPath;
        _context.SaveChanges();
    }

    return RedirectToAction("Profile");
}


}

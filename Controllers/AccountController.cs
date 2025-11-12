using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using start.Models;
using start.Services;
using System.Security.Claims;
using System.Threading.Tasks;
namespace start.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IEmailService _emailService;

        public AccountController(IAuthService authService, IProfileService profileService, IEmailService emailService)
        {
            _authService = authService;
            _profileService = profileService;
            _emailService = emailService;
        }

        #region Login

        [HttpGet]
        public IActionResult Login() => View();

                [HttpPost]
    public async Task <IActionResult> Login(string loginId, string password)
    {
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập Email/Username và mật khẩu");
            return View();
        }

        try
        {
            // 1) Ưu tiên Employee (quản trị, nhân sự nội bộ)
            var emp = _authService.LoginEmployee(loginId, password);
                if (emp != null)
                {
                    // set session cho Employee
                    HttpContext.Session.SetString("EmployeeID", emp.EmployeeID);
                    HttpContext.Session.SetString("EmployeeName", emp.FullName ?? "");
                    HttpContext.Session.SetString("RoleID", emp.RoleID ?? "EM"); // ✅ sửa key thành RoleID

                // điều hướng theo role nội bộ
                if (emp.RoleID == "AD")         // Admin
                    return RedirectToAction("Profile", "Employee");
            else if (emp.RoleID?.Equals("EM", StringComparison.OrdinalIgnoreCase) == true)
        return RedirectToAction("Profile", "Employee"); // <-- đúng controller
                else if (emp.RoleID == "SL")    // Shift Leader (nếu có)
                    return RedirectToAction("Profile", "Employee");
                else if (emp.RoleID == "BM")
                        {

                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.NameIdentifier, emp.EmployeeID),
                                new Claim(ClaimTypes.Name, emp.FullName ?? "User"),
                                new Claim(ClaimTypes.Role, emp.RoleID),
                                new Claim(ClaimTypes.Email, emp.Email ?? "") // Layout BManager sẽ đọc cái này
                            };
                            if (emp.BranchID.HasValue)
                            {
                                claims.Add(new Claim("BranchId", emp.BranchID.Value.ToString()));
                            }

                            var claimsIdentity = new ClaimsIdentity(
                                claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            // Đăng nhập BManager bằng Cookie (Claims)
                            await HttpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity));

                            return RedirectToAction("Index", "BManager");
                        }
                else if (emp.RoleID == "RM")    // Region Manager (nếu có)
                    return RedirectToAction("Profile", "Employee");
            }

            // 2) Nếu không phải Employee, thử Customer (người dùng ngoài)
            var customer = _authService.LoginCustomer(loginId, password);
            if (customer != null)
            {
                HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                HttpContext.Session.SetString("CustomerName", customer.Name ?? "");
                HttpContext.Session.SetString("Role", "Customer");
                return RedirectToAction("Index", "Home");
            }

            // 3) Sai thông tin
            ModelState.AddModelError("", "Sai Email/Username hoặc mật khẩu");
            return View();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(Customer model)
        {
            var user = _authService.Register(model, out string otp, out var errors);

            if (user == null)
            {
                foreach (var kv in errors)
                {
                    ModelState.AddModelError(kv.Key, kv.Value);
                    Console.WriteLine($"Debug: {kv.Key} -> {kv.Value}");
                }
                return View(model);
            }

            _emailService.SendOtp(user.Email!, otp);

            Console.WriteLine($"Debug Register success: CustomerID={user.CustomerID}, OTP={otp}");
            return RedirectToAction("VerifyEmail", new { email = user.Email });
        }


        #endregion

        #region Verify Email

        [HttpGet]
        public IActionResult VerifyEmail(string email)
        {
            return View(new VerifyEmailViewModel { Email = email });
        }

        [HttpPost]
        public IActionResult VerifyEmail(VerifyEmailViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.OtpCode))
            {
                ModelState.AddModelError("", "Email hoặc OTP không được để trống.");
                return View(model);
            }
            try
            {
                _authService.VerifyEmail(model.Email, model.OtpCode);
                TempData["Success"] = "Xác thực email thành công!";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        #endregion

        #region Google Login

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

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                return RedirectToAction("Login");
            }

            var user = _authService.HandleGoogleLogin(email!, name);

            HttpContext.Session.SetInt32("CustomerID", user.CustomerID);
            HttpContext.Session.SetString("CustomerName", user.Name ?? "");

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Logout

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Profile & Edit

        [HttpGet]
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            var user = _profileService.GetUserById(userId.Value);
            if (user == null) return RedirectToAction("Login");

            // Expose whether the account was created via Google (no local password set)
            ViewBag.IsGoogleLogin = string.IsNullOrEmpty(user.Password);
            return View(user);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            var user = _profileService.GetUserById(userId.Value);
            if (user == null) return RedirectToAction("Login");

            var vm = new EditProfileModel
            {
                Name = user.Name,
                Phone = user.Phone,
                Address = user.Address,
                BirthDate = user.BirthDate
            };
            ViewBag.Message = TempData["SuccessMessage"];

            return View(vm);
        }

        [HttpPost]
        public IActionResult EditProfile(EditProfileModel model)
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            if (_profileService.EditProfile(userId.Value, model, out string error))
            {
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", error);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            await _profileService.UploadAvatar(userId.Value, avatar);
            return RedirectToAction("Profile");
        }

        #endregion

        #region Change / Reset Password

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordModel model)
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            if (_authService.ChangePassword(userId.Value, model.CurrentPassword!, model.NewPassword!, out string error))
            {
                ViewBag.Message = "Đổi mật khẩu thành công!";
                return View();
            }

            ModelState.AddModelError("", error);
            return View(model);
        }

        [HttpGet]
        public IActionResult SetPassword()
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");

            return View(new SetPasswordModel());
        }

        [HttpPost]
        public IActionResult SetPassword(SetPasswordModel model)
        {
            int? userId = HttpContext.Session.GetInt32("CustomerID");
            if (userId == null) return RedirectToAction("Login");
            if (!ModelState.IsValid) return View(model);

            if (_authService.SetPassword(userId.Value, model.NewPassword!, out string error))
            {
                TempData["SuccessMessage"] = "Đặt mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", error);
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();


        [HttpGet]
        public IActionResult ResetAgain()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            if (!_authService.SendOtp(email, "reset", out string otp, out string error))
            {
                ViewBag.ErrorMessage = error;
                return View("ResetPassword");
            }

            TempData["Email"] = email;
            ViewBag.SuccessMessage = "Mã OTP đã được gửi lại!";
            return View("ResetPassword");
        }

        [HttpPost]
        public IActionResult SendOtpReset(string email)
        {
            if (!_authService.SendOtp(email, "reset", out string otp, out string error))
            {
                ViewBag.ErrorMessage = error;
                return View("ForgotPassword");
            }

            TempData["Email"] = email;
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword() => View(new ResetPasswordViewModel());

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            var email = TempData["Email"] as string;
            if (email == null) return RedirectToAction("ForgotPassword");

            if (!_authService.ResetPassword(email, model.NewPassword!, out string error))
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            ViewBag.Message = "Mật khẩu đã được đặt lại thành công!";
            return View();
        }

        #endregion
    }
}

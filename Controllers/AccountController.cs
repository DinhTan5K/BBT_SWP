using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using start.Data;
using start.Models;
using start.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
namespace start.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IEmailService _emailService;

        private readonly ApplicationDbContext _db;
        public AccountController(IAuthService authService, IProfileService profileService, IEmailService emailService, ApplicationDbContext db)
        {
            _authService = authService;
            _profileService = profileService;
            _emailService = emailService;
            _db = db;
        }

        #region Login

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string loginId, string password)
        {
            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email/Username và mật khẩu");
                return View();
            }

            try
            {
                var emp = _authService.LoginEmployee(loginId, password);
                if (emp != null)
                {
                    HttpContext.Session.SetString("EmployeeID", emp.EmployeeID);
                    HttpContext.Session.SetString("EmployeeName", emp.FullName ?? "");
                    HttpContext.Session.SetString("Role", emp.RoleID ?? "EM");
                    if (emp.BranchID.HasValue)
                        HttpContext.Session.SetString("BranchId", emp.BranchID.Value.ToString());

                    if (emp.RoleID == "AD")
                        return RedirectToAction("Profile", "Employee");
                    else if (emp.RoleID?.Equals("EM", StringComparison.OrdinalIgnoreCase) == true)
                        return RedirectToAction("Profile", "Employee");
                    else if (emp.RoleID == "SL")
                        return RedirectToAction("Profile", "Employee");
                    else if (emp.RoleID == "BM")
                        return RedirectToAction("Index", "BManager");
                    else if (emp.RoleID == "RM")
                    {
                        // Lấy region info trực tiếp từ Employee.RegionID
                        var regionId = emp.RegionID;
                        if (regionId.HasValue)
                        {
                            HttpContext.Session.SetInt32("RegionID", regionId.Value);
                            var regionName = _db.Regions.AsNoTracking().Where(r => r.RegionID == regionId.Value)
                                              .Select(r => r.RegionName).FirstOrDefault() ?? "Chưa có vùng";
                            HttpContext.Session.SetString("RegionName", regionName);
                        }
                        else
                        {
                            HttpContext.Session.SetInt32("RegionID", 0);
                            HttpContext.Session.SetString("RegionName", "Chưa có vùng");
                        }

                        var avatar = string.IsNullOrWhiteSpace(emp.AvatarUrl)
                ? Url.Content("~/images/default-avatar.png")   // hoặc một URL mặc định
                : emp.AvatarUrl;
                        HttpContext.Session.SetString("AvatarUrl", avatar);

                        return RedirectToAction("RegionHome", "Region");
                    }
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

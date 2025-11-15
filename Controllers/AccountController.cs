using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using start.Data;
using start.Models;
using start.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
namespace start.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IEmailService _emailService;
        private readonly IAdminSecurityService _adminSecurityService;
        private readonly ApplicationDbContext _db;

        private const string PendingAdmin2FAKey = "PendingAdmin2FA";
        private const string PendingAdmin2FAEmailKey = "PendingAdmin2FAEmail";

        public AccountController(
            IAuthService authService,
            IProfileService profileService,
            IEmailService emailService,
            IAdminSecurityService adminSecurityService,
            ApplicationDbContext db)
        {
            _authService = authService;
            _profileService = profileService;
            _emailService = emailService;
            _adminSecurityService = adminSecurityService;
            _db = db;
        }

        #region Login

        [HttpGet]
        public IActionResult Login() => View();
        
  [HttpPost]
    public async Task<IActionResult> Login(string loginId, string password)
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
                if (emp.RoleID == "AD" && !string.IsNullOrEmpty(emp.EmployeeID))
                {
                    if (await _adminSecurityService.IsTwoFactorEnabledAsync(emp.EmployeeID))
                    {
                        var otpResult = await _adminSecurityService.SendOtpAsync(emp, AdminOtpPurpose.Login);
                        if (!otpResult.Succeeded)
                        {
                            ModelState.AddModelError("", otpResult.Message);
                            return View();
                        }

                        HttpContext.Session.Remove(PendingAdmin2FAKey);
                        HttpContext.Session.Remove(PendingAdmin2FAEmailKey);
                        HttpContext.Session.SetString(PendingAdmin2FAKey, emp.EmployeeID);
                        HttpContext.Session.SetString(PendingAdmin2FAEmailKey, emp.Email ?? string.Empty);
                        await HttpContext.Session.CommitAsync();

                        TempData["InfoMessage"] = otpResult.Message;
                        return RedirectToAction("AdminTwoFactor");
                    }
                }

                return await SignInEmployeeAndRedirectAsync(emp);
            }

            // 2) Nếu không phải Employee, thử Customer (người dùng ngoài)
            try
            {
                var customer = _authService.LoginCustomer(loginId, password);
                if (customer != null)
                {
                    // Log thành công - UTCID5-01
                    System.Diagnostics.Debug.WriteLine("Login successful");
                    
                    // Tạo Claims giống Google login để đảm bảo authentication hoạt động
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, customer.Name ?? ""),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, customer.Email ?? ""),
                        new System.Security.Claims.Claim("Role", "CU")
                    };

                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "CustomerScheme");
                    var principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

                    // Sign in với CustomerScheme để tạo authentication cookie
                    // KHÔNG sign out các schemes khác - mỗi scheme có cookie riêng nên không conflict
                    await HttpContext.SignInAsync("CustomerScheme", principal);
                    
                    // Set Session để tương thích với code cũ
                    HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                    HttpContext.Session.SetString("CustomerName", customer.Name ?? "");
                    HttpContext.Session.SetString("Role", "Customer");
                    
                    // Đảm bảo session được commit trước khi redirect
                    await HttpContext.Session.CommitAsync();
                    
                    // Clear ModelState để tránh validation errors khi redirect
                    ModelState.Clear();
                    
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception customerEx)
            {
                // Nếu là lỗi email chưa xác thực, hiển thị thông báo cụ thể
                if (customerEx.Message.Contains("Email chưa xác thực") || customerEx.Message.Contains("chưa xác thực"))
                {
                    ModelState.AddModelError("", customerEx.Message);
                    return View();
                }
                // Nếu là lỗi khác, throw lại để catch bên ngoài xử lý
                throw;
            }

            // 3) Sai thông tin (đã log trong AuthService)
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

        private async Task<IActionResult> SignInEmployeeAndRedirectAsync(Employee emp)
        {
            // Log thành công - UTCID5-01
            System.Diagnostics.Debug.WriteLine($"Login successful: EmployeeID={emp.EmployeeID}, Role={emp.RoleID}");

            var employeeId = emp.EmployeeID ?? string.Empty;
            var fullName = emp.FullName ?? string.Empty;
            var email = emp.Email ?? string.Empty;
            var roleId = emp.RoleID ?? "EM";

            // Xác định scheme dựa trên role - mỗi role có scheme riêng
            var authScheme = roleId switch
            {
                "AD" => "AdminScheme",
                "BM" => "BranchManagerScheme",
                "EM" => "EmployeeScheme",
                "MK" => "MarketingScheme",
                "RM" => "RegionManagerScheme",
                "SL" => "ShiftLeaderScheme",
                "SP" => "ShipperScheme",
                _ => "EmployeeScheme" // Fallback cho các role khác
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employeeId),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim("Role", roleId),
                new Claim("RoleID", roleId)
            };

            var claimsIdentity = new ClaimsIdentity(claims, authScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(authScheme, principal);

            HttpContext.Session.SetString("EmployeeID", employeeId);
            HttpContext.Session.SetString("EmployeeName", fullName);
            // Chỉ set email vào session nếu có giá trị
            if (!string.IsNullOrWhiteSpace(email))
            {
                HttpContext.Session.SetString("Email", email);
            }
            else
            {
                HttpContext.Session.Remove("Email");
            }
            HttpContext.Session.SetString("Role", roleId);
            HttpContext.Session.SetString("RoleID", roleId);

            if (emp.BranchID.HasValue)
            {
                HttpContext.Session.SetString("BranchId", emp.BranchID.Value.ToString());
            }
            else
            {
                HttpContext.Session.Remove("BranchId");
            }

            // Set RegionID and RegionName if employee has region
            if (emp.RegionID.HasValue)
            {
                HttpContext.Session.SetInt32("RegionID", emp.RegionID.Value);
                
                // Get region name from database
                var region = await _db.Regions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RegionID == emp.RegionID.Value);
                if (region != null)
                {
                    HttpContext.Session.SetString("RegionName", region.RegionName ?? "Unknown");
                }
                else
                {
                    HttpContext.Session.SetString("RegionName", "Unknown");
                }
            }
            else
            {
                HttpContext.Session.Remove("RegionID");
                HttpContext.Session.Remove("RegionName");
            }

            HttpContext.Session.Remove(PendingAdmin2FAKey);
            HttpContext.Session.Remove(PendingAdmin2FAEmailKey);

            await HttpContext.Session.CommitAsync();

            return roleId switch
            {
                "AD" => RedirectToAction("Dashboard", "Admin"),
                "BM" => RedirectToAction("Index", "BManager"),
                "SL" => RedirectToAction("Profile", "Employee"),
                "RM" => RedirectToAction("RegionHome", "Region"),
                _ => RedirectToAction("Profile", "Employee")
            };
        }

        [HttpGet]
        public IActionResult AdminTwoFactor()
        {
            var employeeId = HttpContext.Session.GetString(PendingAdmin2FAKey);
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login");
            }

            ViewBag.MaskedEmail = MaskEmail(HttpContext.Session.GetString(PendingAdmin2FAEmailKey));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminTwoFactor(string otp)
        {
            var employeeId = HttpContext.Session.GetString(PendingAdmin2FAKey);
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login");
            }

            ViewBag.MaskedEmail = MaskEmail(HttpContext.Session.GetString(PendingAdmin2FAEmailKey));

            if (string.IsNullOrWhiteSpace(otp))
            {
                ModelState.AddModelError("", "Vui lòng nhập mã OTP.");
                return View();
            }

            var verification = await _adminSecurityService.VerifyOtpAsync(employeeId, otp.Trim());
            if (!verification.Succeeded)
            {
                if (verification.IsLocked)
                {
                    TempData["ErrorMessage"] = verification.Message;
                    HttpContext.Session.Remove(PendingAdmin2FAKey);
                    HttpContext.Session.Remove(PendingAdmin2FAEmailKey);
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", verification.Message);
                return View();
            }

            var emp = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (emp == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản quản trị viên.");
                return View();
            }

            return await SignInEmployeeAndRedirectAsync(emp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendAdminTwoFactor()
        {
            var employeeId = HttpContext.Session.GetString(PendingAdmin2FAKey);
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login");
            }

            var emp = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (emp == null)
            {
                HttpContext.Session.Remove(PendingAdmin2FAKey);
                HttpContext.Session.Remove(PendingAdmin2FAEmailKey);
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản quản trị viên.";
                return RedirectToAction("Login");
            }

            var result = await _adminSecurityService.SendOtpAsync(emp, AdminOtpPurpose.Login);
            if (result.Succeeded)
            {
                TempData["InfoMessage"] = "Đã gửi lại mã OTP.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("AdminTwoFactor");
        }

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
                }
                return View(model);
            }

            _emailService.SendOtp(user.Email!, otp);

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
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded) return RedirectToAction("Login");

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                return RedirectToAction("Login");
            }

            var user = _authService.HandleGoogleLogin(email!, name);

            // Tạo Claims giống login thông thường để đảm bảo authentication hoạt động
            var customerClaims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.CustomerID.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name ?? ""),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? ""),
                new System.Security.Claims.Claim("Role", "CU")
            };

            var claimsIdentity = new System.Security.Claims.ClaimsIdentity(customerClaims, "CustomerScheme");
            var principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

            // Sign in với CustomerScheme để tạo authentication cookie
            await HttpContext.SignInAsync("CustomerScheme", principal);

            // Set Session để tương thích với code cũ
            HttpContext.Session.SetInt32("CustomerID", user.CustomerID);
            HttpContext.Session.SetString("CustomerName", user.Name ?? "");
            HttpContext.Session.SetString("Role", "Customer");

            // Đảm bảo session được commit trước khi redirect
            await HttpContext.Session.CommitAsync();

            return RedirectToAction("Index", "Home");
        }
    


        #endregion

        #region Logout

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Danh sách tất cả các schemes để kiểm tra
            var allSchemes = new[]
            {
                "AdminScheme",
                "BranchManagerScheme",
                "EmployeeScheme",
                "MarketingScheme",
                "RegionManagerScheme",
                "ShiftLeaderScheme",
                "ShipperScheme",
                "CustomerScheme"
            };

            // Danh sách các schemes đang active (có thể có nhiều nếu user đăng nhập nhiều role)
            var activeSchemes = new List<string>();

            // Kiểm tra User.Identity để xác định scheme đang active
            if (User?.Identity?.IsAuthenticated == true)
            {
                var authType = User.Identity.AuthenticationType;
                if (!string.IsNullOrEmpty(authType) && allSchemes.Contains(authType))
                {
                    activeSchemes.Add(authType);
                }
            }

            // Kiểm tra từng scheme để tìm tất cả schemes đang active (fallback)
            foreach (var scheme in allSchemes)
            {
                var result = await HttpContext.AuthenticateAsync(scheme);
                if (result?.Succeeded == true && !activeSchemes.Contains(scheme))
                {
                    activeSchemes.Add(scheme);
                }
            }

            // Clear session keys dựa trên scheme
            foreach (var scheme in activeSchemes)
            {
                if (scheme == "CustomerScheme")
                {
                    HttpContext.Session.Remove("CustomerID");
                    HttpContext.Session.Remove("CustomerName");
                    HttpContext.Session.Remove("Role");
                }
                else
                {
                    // Tất cả employee schemes dùng chung session keys
                    HttpContext.Session.Remove("EmployeeID");
                    HttpContext.Session.Remove("EmployeeName");
                    HttpContext.Session.Remove("Role");
                    HttpContext.Session.Remove("RoleID");
                    HttpContext.Session.Remove("BranchId");
                    HttpContext.Session.Remove("RegionID");
                    HttpContext.Session.Remove("RegionName");
                }
            }

            // Sign out tất cả các schemes đang active (cho phép logout nhiều role cùng lúc nếu cần)
            foreach (var scheme in activeSchemes)
            {
                await HttpContext.SignOutAsync(scheme);
            }

            // Nếu không có scheme nào active, vẫn clear session để đảm bảo
            if (activeSchemes.Count == 0)
            {
                HttpContext.Session.Clear();
            }

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
    
    private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "(chưa cập nhật)";

            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                return "****" + (atIndex >= 0 ? email[atIndex..] : string.Empty);
            }

            var prefix = email.Substring(0, Math.Min(2, atIndex));
            var domain = atIndex >= 0 ? email[atIndex..] : string.Empty;
            return $"{prefix}****{domain}";
        }
    }
}

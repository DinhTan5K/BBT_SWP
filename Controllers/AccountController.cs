using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using start.Models;
using start.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Google;
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
        public IActionResult Login()
        {
            // Kiểm tra nếu có lỗi từ Google OAuth
            if (Request.Query.ContainsKey("error") && Request.Query["error"] == "google_connection_failed")
            {
                ViewBag.ErrorMessage = "Không thể kết nối với Google. Vui lòng kiểm tra kết nối internet và thử lại.";
            }
            return View();
        }

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
                // Log thành công - UTCID5-01
                System.Diagnostics.Debug.WriteLine($"Login successful: EmployeeID={emp.EmployeeID}, Role={emp.RoleID}");
                
                // Xác định scheme dựa trên role
                string authScheme = emp.RoleID == "AD" ? "AdminScheme" : "EmployeeScheme";
                
                // Tạo Claims cho Employee
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, emp.EmployeeID ?? ""),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, emp.FullName ?? ""),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, emp.Email ?? ""),
                    new System.Security.Claims.Claim("Role", emp.RoleID ?? "EM"),
                    new System.Security.Claims.Claim("RoleID", emp.RoleID ?? "EM")
                };

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, authScheme);
                var principal = new System.Security.Claims.ClaimsPrincipal(claimsIdentity);

                // Sign in với đúng scheme để tạo authentication cookie
                // KHÔNG sign out các schemes khác - mỗi scheme có cookie riêng nên không conflict
                await HttpContext.SignInAsync(authScheme, principal);
                
                // Set Session để tương thích với code cũ
                HttpContext.Session.SetString("EmployeeID", emp.EmployeeID ?? "");
                HttpContext.Session.SetString("EmployeeName", emp.FullName ?? "");
                HttpContext.Session.SetString("Role", emp.RoleID ?? "EM");
                HttpContext.Session.SetString("RoleID", emp.RoleID ?? "EM");
                
                if (emp.BranchID.HasValue)
                {
                    HttpContext.Session.SetString("BranchId", emp.BranchID.Value.ToString());
                }

                // Commit session
                await HttpContext.Session.CommitAsync();

                // điều hướng theo role nội bộ
                if (emp.RoleID == "AD")         // Admin
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (emp.RoleID?.Equals("EM", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return RedirectToAction("Profile", "Employee");
                }
                else if (emp.RoleID == "SL")    // Shift Leader (nếu có)
                {
                    return RedirectToAction("Profile", "Employee");
                }
                else if (emp.RoleID == "BM")    // Branch Manager (nếu có)
                {
                    return RedirectToAction("Index", "BManager");
                }
                else if (emp.RoleID == "RM")    // Region Manager (nếu có)
                {
                    return RedirectToAction("Profile", "Employee");
                }
                else
                {
                    return RedirectToAction("Profile", "Employee"); // Default
                }
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
            // Xác định scheme hiện tại của user dựa trên User.Identity
            string? currentScheme = null;
            
            // Kiểm tra User.Identity để xác định scheme đang active
            if (User?.Identity?.IsAuthenticated == true)
            {
                // Lấy authentication type từ User.Identity
                var authType = User.Identity.AuthenticationType;
                
                if (authType == "AdminScheme")
                {
                    currentScheme = "AdminScheme";
                    // Clear session keys của admin
                    HttpContext.Session.Remove("EmployeeID");
                    HttpContext.Session.Remove("EmployeeName");
                    HttpContext.Session.Remove("Role");
                    HttpContext.Session.Remove("RoleID");
                    HttpContext.Session.Remove("BranchId");
                }
                else if (authType == "EmployeeScheme")
                {
                    currentScheme = "EmployeeScheme";
                    // Clear session keys của employee
                    HttpContext.Session.Remove("EmployeeID");
                    HttpContext.Session.Remove("EmployeeName");
                    HttpContext.Session.Remove("Role");
                    HttpContext.Session.Remove("RoleID");
                    HttpContext.Session.Remove("BranchId");
                }
                else if (authType == "CustomerScheme")
                {
                    currentScheme = "CustomerScheme";
                    // Clear session keys của customer
                    HttpContext.Session.Remove("CustomerID");
                    HttpContext.Session.Remove("CustomerName");
                    HttpContext.Session.Remove("Role");
                }
            }
            
            // Nếu không tìm thấy từ User.Identity, thử kiểm tra từng scheme
            if (string.IsNullOrEmpty(currentScheme))
            {
                // Kiểm tra từng scheme để tìm scheme đang active (fallback)
                var adminResult = await HttpContext.AuthenticateAsync("AdminScheme");
                if (adminResult?.Succeeded == true)
                {
                    currentScheme = "AdminScheme";
                    HttpContext.Session.Remove("EmployeeID");
                    HttpContext.Session.Remove("EmployeeName");
                    HttpContext.Session.Remove("Role");
                    HttpContext.Session.Remove("RoleID");
                    HttpContext.Session.Remove("BranchId");
                }
                else
                {
                    var employeeResult = await HttpContext.AuthenticateAsync("EmployeeScheme");
                    if (employeeResult?.Succeeded == true)
                    {
                        currentScheme = "EmployeeScheme";
                        HttpContext.Session.Remove("EmployeeID");
                        HttpContext.Session.Remove("EmployeeName");
                        HttpContext.Session.Remove("Role");
                        HttpContext.Session.Remove("RoleID");
                        HttpContext.Session.Remove("BranchId");
                    }
                    else
                    {
                        var customerResult = await HttpContext.AuthenticateAsync("CustomerScheme");
                        if (customerResult?.Succeeded == true)
                        {
                            currentScheme = "CustomerScheme";
                            HttpContext.Session.Remove("CustomerID");
                            HttpContext.Session.Remove("CustomerName");
                            HttpContext.Session.Remove("Role");
                        }
                    }
                }
            }
            
            // Chỉ sign out scheme của user hiện tại, KHÔNG sign out tất cả
            if (!string.IsNullOrEmpty(currentScheme))
            {
                await HttpContext.SignOutAsync(currentScheme);
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
    }
}

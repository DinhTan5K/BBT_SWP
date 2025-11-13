using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Linq;
using start.Data;
using start.Models;
using start.Services;
using System.ComponentModel;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ClosedXML.Excel;


namespace start.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminScheme")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmployeeProfileService _employeeProfileService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IAuthService _authService;
        private readonly IAdminSecurityService _adminSecurityService;

        public AdminController(
            ApplicationDbContext db,
            IEmployeeProfileService employeeProfileService,
            ICloudinaryService cloudinaryService,
            IAuthService authService,
            IAdminSecurityService adminSecurityService)
        {
            _db = db;
            _employeeProfileService = employeeProfileService;
            _cloudinaryService = cloudinaryService;
            _authService = authService;
            _adminSecurityService = adminSecurityService;
        }

        // Lấy EmployeeID và Role từ Claims
        private string? CurrentEmpId => User.FindFirst("EmployeeID")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? CurrentRole => User.FindFirst("Role")?.Value?.Trim().ToUpperInvariant();


        // GET /Admin/Dashboard
        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);

            if (emp == null) return NotFound();

            // Thống kê tổng quan cho dashboard
            var totalEmployees = _db.Employees.Where(e => e.RoleID != "AD" && e.IsActive).Count();
            var totalOrders = _db.Orders.Count();
            var totalCustomers = _db.Customers.Count();
            var totalProducts = _db.Products.Count();

            // Thống kê đơn hàng theo trạng thái
            var pendingOrders = _db.Orders.Where(o => o.Status == "Chờ xác nhận").Count();
            var confirmedOrders = _db.Orders.Where(o => o.Status == "Đã xác nhận").Count();
            var shippingOrders = _db.Orders.Where(o => o.Status == "Đang giao").Count();
            var deliveredOrders = _db.Orders.Where(o => o.Status == "Đã giao").Count();
            var cancelledOrders = _db.Orders.Where(o => o.Status == "Đã hủy").Count();
            var pendingRefundOrders = _db.Orders.Where(o => o.Status == "Chờ hoàn tiền").Count();
            var refundedOrders = _db.Orders.Where(o => o.Status == "Đã hoàn tiền").Count();

            // Doanh thu tháng này (tính đơn đã giao + đơn đã xác nhận nếu thanh toán Momo)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var revenueThisMonth = _db.Orders
                .Where(o => o.CreatedAt.Month == currentMonth &&
                           o.CreatedAt.Year == currentYear &&
                           (o.Status == "Đã giao" ||
                            (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo")))
                .Sum(o => (decimal?)o.Total) ?? 0;

            // Doanh thu tháng trước (tính đơn đã giao + đơn đã xác nhận nếu thanh toán Momo)
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;
            var revenueLastMonth = _db.Orders
                .Where(o => o.CreatedAt.Month == lastMonth &&
                           o.CreatedAt.Year == lastMonthYear &&
                           (o.Status == "Đã giao" ||
                            (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo")))
                .Sum(o => (decimal?)o.Total) ?? 0;

            ViewBag.Employee = emp;
            ViewBag.ActiveMenu = "Dashboard";
            ViewBag.CurrentRole = CurrentRole; // Truyền role vào ViewBag cho layout
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ConfirmedOrders = confirmedOrders;
            ViewBag.ShippingOrders = shippingOrders;
            ViewBag.DeliveredOrders = deliveredOrders;
            ViewBag.CancelledOrders = cancelledOrders;
            ViewBag.PendingRefundOrders = pendingRefundOrders;
            ViewBag.RefundedOrders = refundedOrders;
            ViewBag.RevenueThisMonth = revenueThisMonth;
            ViewBag.RevenueLastMonth = revenueLastMonth;

            // Dữ liệu cho biểu đồ tròn - đơn hàng theo trạng thái
            var orderStatusData = new
            {
                Labels = new[] { "Chờ xác nhận", "Đã xác nhận", "Đang giao", "Đã giao", "Đã hủy", "Chờ hoàn tiền", "Đã hoàn tiền" },
                Data = new[]
                {
                    pendingOrders,
                    confirmedOrders,
                    shippingOrders,
                    deliveredOrders,
                    cancelledOrders,
                    pendingRefundOrders,
                    refundedOrders
                }
            };

            // Dữ liệu cho biểu đồ cột - doanh thu 6 tháng gần nhất (chỉ tính đơn đã giao)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var monthlyRevenueData = last6Months.Select(month => new
            {
                Month = month.ToString("MK/yyyy"),
                Revenue = _db.Orders
                    .Where(o => o.CreatedAt.Month == month.Month &&
                               o.CreatedAt.Year == month.Year &&
                               (o.Status == "Đã giao" ||
                                (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo")))
                    .Sum(o => (decimal?)o.Total) ?? 0
            }).ToList();

            ViewBag.OrderStatusData = orderStatusData;
            ViewBag.MonthlyRevenueData = monthlyRevenueData;

            // ========== THÊM CÁC THỐNG KÊ MỚI ==========

            // 1. Top 5 sản phẩm bán chạy (từ đơn đã giao + đơn đã xác nhận nếu thanh toán Momo)
            var deliveredOrderIds = _db.Orders
                .Where(o => o.Status == "Đã giao" ||
                           (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo"))
                .Select(o => o.OrderID)
                .ToList();

            var topProducts = _db.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Where(od => od.Product != null && deliveredOrderIds.Contains(od.OrderID))
                .GroupBy(od => new { od.ProductID, ProductName = od.Product != null ? od.Product.ProductName : "Không xác định" })
                .Select(g => new
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Total)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            // 2. Đơn hàng gần đây (10 đơn mới nhất)
            var recentOrders = _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new
                {
                    OrderID = o.OrderID,
                    OrderCode = o.OrderCode ?? o.OrderID.ToString(),
                    CustomerName = o.Customer != null ? o.Customer.Name : "Không xác định",
                    BranchName = o.Branch != null ? o.Branch.Name : "Không xác định",
                    Total = o.Total,
                    Status = o.Status ?? "Chưa xác định",
                    CreatedAt = o.CreatedAt
                })
                .ToList();

            // 3. Yêu cầu chờ duyệt
            var pendingApprovals = _db.DiscountRequests
                .Where(dr => dr.Status == RequestStatus.Pending)
                .Count() +
                _db.NewsRequests
                .Where(nr => nr.Status == RequestStatus.Pending)
                .Count() +
                _db.EmployeeBranchRequests
                .Where(ebr => ebr.Status == RequestStatus.Pending)
                .Count() +
                _db.ProductRequests
                .Where(pr => pr.Status == RequestStatus.Pending)
                .Count() +
                _db.CategoryRequests
                .Where(cr => cr.Status == RequestStatus.Pending)
                .Count() +
                _db.BranchRequests
                .Where(br => br.Status == RequestStatus.Pending)
                .Count();

            // 4. Hoạt động hôm nay
            var today = DateTime.Now.Date;
            var todayOrders = _db.Orders
                .Where(o => o.CreatedAt.Date == today)
                .Count();
            var todayRevenue = _db.Orders
                .Where(o => o.CreatedAt.Date == today &&
                           (o.Status == "Đã giao" ||
                            (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo")))
                .Sum(o => (decimal?)o.Total) ?? 0;
            var todayCustomers = _db.Customers
                .Where(c => c.CreatedAt.Date == today)
                .Count();

            // 5. Khách hàng mới (7 ngày gần nhất)
            var newCustomersThisWeek = _db.Customers
                .Where(c => c.CreatedAt >= DateTime.Now.AddDays(-7))
                .Count();

            // 6. Mã giảm giá đang hoạt động
            var activeDiscounts = _db.Discounts
                .Where(d => d.IsActive)
                .Count();

            // 7. Sản phẩm không hoạt động (IsActive = false)
            var inactiveProducts = _db.Products
                .Where(p => !p.IsActive)
                .Count();

            // 7.1. Tổng số danh mục sản phẩm
            var totalCategories = _db.ProductCategories.Count();

            // 7.2. Thống kê tin tức
            var totalNews = _db.News.Count();
            var newsThisWeek = _db.News
                .Where(n => n.CreatedAt >= DateTime.Now.AddDays(-7))
                .Count();

            // 7.3. Top sản phẩm được yêu thích
            var topWishlistedProducts = _db.Wishlist
                .AsNoTracking()
                .Include(w => w.Product)
                .Where(w => w.Product != null)
                .GroupBy(w => new { w.ProductID, ProductName = w.Product != null ? w.Product.ProductName : "Không xác định" })
                .Select(g => new
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    WishlistCount = g.Count()
                })
                .OrderByDescending(x => x.WishlistCount)
                .Take(5)
                .ToList();

            // 8. Đơn hàng cần xử lý (chờ xác nhận + đang giao)
            var ordersNeedingAttention = pendingOrders + shippingOrders;

            // 9. Tổng doanh thu (tất cả đơn đã giao + đơn đã xác nhận nếu thanh toán Momo)
            var totalRevenue = _db.Orders
                .Where(o => o.Status == "Đã giao" ||
                           (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo"))
                .Sum(o => (decimal?)o.Total) ?? 0;

            // 10. Thống kê theo chi nhánh (top 3)
            var allBranches = _db.Branches
                .AsNoTracking()
                .ToList();

            var allOrdersForBranch = _db.Orders
                .AsNoTracking()
                .ToList();

            var branchStats = allBranches
                .Select(branch => new
                {
                    BranchID = branch.BranchID,
                    BranchName = branch.Name ?? "Không xác định",
                    OrderCount = allOrdersForBranch.Count(o => o.BranchID == branch.BranchID),
                    Revenue = allOrdersForBranch
                        .Where(o => o.BranchID == branch.BranchID &&
                                   (o.Status == "Đã giao" ||
                                    (o.Status == "Đã xác nhận" && o.PaymentMethod != null && o.PaymentMethod.ToLower().Trim() == "momo")))
                        .Sum(o => o.Total)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(3)
                .ToList();

            ViewBag.TopProducts = topProducts;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.PendingApprovals = pendingApprovals;
            ViewBag.TodayOrders = todayOrders;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.TodayCustomers = todayCustomers;
            ViewBag.NewCustomersThisWeek = newCustomersThisWeek;
            ViewBag.ActiveDiscounts = activeDiscounts;
            ViewBag.InactiveProducts = inactiveProducts;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalNews = totalNews;
            ViewBag.NewsThisWeek = newsThisWeek;
            ViewBag.TopWishlistedProducts = topWishlistedProducts;
            ViewBag.OrdersNeedingAttention = ordersNeedingAttention;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.BranchStats = branchStats;

            return View(emp);
        }

        // GET /Admin/Profile
        public async Task<IActionResult> Profile()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            var emp = await _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .Include(e => e.Role)
                         .SingleOrDefaultAsync(e => e.EmployeeID == CurrentEmpId);

            if (emp == null) return NotFound();

            var security = await _adminSecurityService.GetOrCreateAsync(emp.EmployeeID ?? string.Empty);
            ViewBag.AdminSecurity = security;

            ViewBag.ActiveMenu = "Profile";
            return View(emp);
        }

        // POST /Admin/UploadAvatar
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            if (avatar != null && avatar.Length > 0)
            {
                var success = await _employeeProfileService.UploadAvatar(CurrentEmpId, avatar);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật ảnh đại diện thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật ảnh đại diện.";
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTwoFactorSetupOtp()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
                return RedirectToAction("Login", "Account");

            var admin = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == CurrentEmpId);

            if (admin == null)
                return RedirectToAction("Login", "Account");

            var result = await _adminSecurityService.SendOtpAsync(admin, AdminOtpPurpose.Setup);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactorSetup(string otp)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(otp))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã OTP.";
                return RedirectToAction("Profile");
            }

            var verification = await _adminSecurityService.VerifyOtpAsync(CurrentEmpId, otp.Trim());
            if (!verification.Succeeded)
            {
                TempData["ErrorMessage"] = verification.Message;
                if (verification.IsLocked)
                {
                    TempData["ErrorMessage"] = verification.Message;
                }
                return RedirectToAction("Profile");
            }

            await _adminSecurityService.EnableTwoFactorAsync(CurrentEmpId);
            TempData["SuccessMessage"] = "Đã bật 2FA cho tài khoản quản trị viên.";
            return RedirectToAction("Profile");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDisableTwoFactorOtp()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
                return RedirectToAction("Login", "Account");

            var admin = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == CurrentEmpId);

            if (admin == null)
                return RedirectToAction("Login", "Account");

            var result = await _adminSecurityService.SendOtpAsync(admin, AdminOtpPurpose.Disable);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(string otp)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(otp))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã OTP để xác nhận tắt 2FA.";
                return RedirectToAction("Profile");
            }

            var verification = await _adminSecurityService.VerifyOtpAsync(CurrentEmpId, otp.Trim());
            if (!verification.Succeeded)
            {
                TempData["ErrorMessage"] = verification.Message;
                if (verification.IsLocked)
                {
                    TempData["ErrorMessage"] = verification.Message;
                }
                return RedirectToAction("Profile");
            }

            await _adminSecurityService.DisableTwoFactorAsync(CurrentEmpId);
            TempData["SuccessMessage"] = "Đã tắt 2FA cho tài khoản quản trị viên.";
            return RedirectToAction("Profile");
        }

        // GET /Admin/Employees - Danh sách tất cả nhân viên (không bao gồm Admin) với tabs để chuyển sang Quản lý vai trò
        public async Task<IActionResult> Employees(string tab = "employees")
        {
            // Thứ tự ưu tiên vai trò: RM, BM, SL, EM, SP, và các role khác
            var roleOrder = new Dictionary<string, int>
            {
                { "RM", 1 },
                { "BM", 2 },
                { "SL", 3 },
                { "EM", 4 },
                { "SP", 5 }
            };

            var employees = _db.Employees
                              .AsNoTracking()
                              .Include(e => e.Branch)
                              .Include(e => e.Role)
                              .Where(e => e.RoleID != "AD") // Loại bỏ Admin
                              .ToList()
                              .OrderBy(e => roleOrder.GetValueOrDefault(e.RoleID ?? "", 99)) // Sắp xếp theo thứ tự vai trò
                              .ThenBy(e => e.FullName) // Sau đó sắp xếp theo tên
                              .ToList();

            // Load dữ liệu vai trò nếu đang ở tab roles
            if (tab == "roles")
            {
                // Lấy tất cả vai trò, loại bỏ AD (Admin) vì đây là vai trò hệ thống
                var roles = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.RoleID != "AD") // Loại bỏ vai trò AD
                    .OrderBy(r => r.RoleID)
                    .ToListAsync();

                // Tính số lượng nhân viên cho mỗi vai trò
                var rolesWithStats = new List<dynamic>();
                foreach (var role in roles)
                {
                    var activeCount = await _db.Employees.CountAsync(e => e.RoleID == role.RoleID && e.IsActive);
                    var totalCount = await _db.Employees.CountAsync(e => e.RoleID == role.RoleID);
                    rolesWithStats.Add(new
                    {
                        Role = role,
                        ActiveEmployeeCount = activeCount,
                        TotalEmployeeCount = totalCount,
                        InactiveEmployeeCount = totalCount - activeCount
                    });
                }

                ViewBag.RolesWithStats = rolesWithStats;
                ViewBag.CurrentRole = CurrentRole;
            }
            else if (tab == "regionManagers")
            {
                var regionManagers = await _db.Employees
                    .AsNoTracking()
                    .Include(e => e.Region)
                    .Include(e => e.Role)
                    .Where(e => e.RoleID == "RM")
                    .OrderBy(e => e.FullName)
                    .ToListAsync();

                ViewBag.RegionManagers = regionManagers;
                ViewBag.RMActiveCount = regionManagers.Count(e => e.IsActive);
                ViewBag.RMInactiveCount = regionManagers.Count(e => !e.IsActive);
                ViewBag.RMTotalCount = regionManagers.Count;
            }
            else if (tab == "marketingManagers")
            {
                var marketingManagers = await _db.Employees
                    .AsNoTracking()
                    .Include(e => e.Role)
                    .Where(e => e.RoleID == "MK")
                    .OrderBy(e => e.FullName)
                    .ToListAsync();

                // Đếm tổng số nhân viên Marketing (MK) trong hệ thống
                var totalMarketingEmployees = await _db.Employees
                    .CountAsync(e => e.RoleID == "MK" && e.IsActive);

                ViewBag.MarketingManagers = marketingManagers;
                ViewBag.TotalMarketingEmployees = totalMarketingEmployees;
                ViewBag.MKActiveCount = marketingManagers.Count(e => e.IsActive);
                ViewBag.MKInactiveCount = marketingManagers.Count(e => !e.IsActive);
                ViewBag.MKTotalCount = marketingManagers.Count;
            }

            ViewBag.ActiveMenu = "Employees";
            ViewBag.ActiveTab = tab;
            return View(employees);
        }

        // GET /Admin/ViewEmployee/{id} - Xem chi tiết nhân viên (cho admin)
        public IActionResult ViewEmployee(string id)
        {
            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .Include(e => e.Role)
                         .SingleOrDefault(e => e.EmployeeID == id);

            if (emp == null) return NotFound();

            // Lấy contract hiện tại (nếu có)
            var currentContract = _db.Contracts
                .AsNoTracking()
                .Where(c => c.EmployeeId == id && c.Status == "Hiệu lực")
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault();

            ViewBag.CurrentContract = currentContract;
            ViewBag.ActiveMenu = "Employees";
            return View(emp);
        }

        // GET /Admin/CreateContract/{employeeId} - Form tạo contract
        public IActionResult CreateContract(string employeeId)
        {
            var emp = _db.Employees
                         .AsNoTracking()
                         .FirstOrDefault(e => e.EmployeeID == employeeId);

            if (emp == null)
                return Json(new { success = false, message = "Nhân viên không tồn tại" });

            ViewBag.Employee = emp;
            return PartialView("_CreateContractModal", new Contract { EmployeeId = employeeId });
        }

        // POST /Admin/CreateContract - Tạo contract mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContract(
            [FromForm] string employeeId,
            [FromForm] string contractNumber,
            [FromForm] string contractType,
            [FromForm] DateTime startDate,
            [FromForm] DateTime? endDate,
            [FromForm] string paymentType,
            [FromForm] decimal baseRate,
            [FromForm] string status)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(employeeId))
                    return Json(new { success = false, message = "Mã nhân viên không được để trống" });

                if (string.IsNullOrWhiteSpace(contractNumber))
                    return Json(new { success = false, message = "Số hợp đồng không được để trống" });

                if (string.IsNullOrWhiteSpace(contractType))
                    return Json(new { success = false, message = "Loại hợp đồng không được để trống" });

                if (string.IsNullOrWhiteSpace(paymentType))
                    return Json(new { success = false, message = "Cách thức tính lương không được để trống" });

                if (baseRate <= 0)
                    return Json(new { success = false, message = "Mức lương phải lớn hơn 0" });

                // Kiểm tra employee có tồn tại không
                var employee = await _db.Employees.FindAsync(employeeId);
                if (employee == null)
                    return Json(new { success = false, message = "Nhân viên không tồn tại" });

                // Kiểm tra contract number có trùng không
                var existingContract = await _db.Contracts
                    .FirstOrDefaultAsync(c => c.ContractNumber == contractNumber.Trim());
                if (existingContract != null)
                    return Json(new { success = false, message = "Số hợp đồng đã tồn tại" });

                // Nếu có contract "Hiệu lực" cũ, set status = "Hết hạn"
                var activeContracts = await _db.Contracts
                    .Where(c => c.EmployeeId == employeeId && c.Status == "Hiệu lực")
                    .ToListAsync();
                foreach (var oldContract in activeContracts)
                {
                    oldContract.Status = "Hết hạn";
                    oldContract.UpdatedAt = DateTime.UtcNow;
                }

                // Tạo contract mới
                var contract = new Contract
                {
                    EmployeeId = employeeId,
                    ContractNumber = contractNumber.Trim(),
                    ContractType = contractType.Trim(),
                    StartDate = startDate,
                    EndDate = endDate,
                    PaymentType = paymentType.Trim(),
                    BaseRate = baseRate,
                    Status = status.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.Contracts.Add(contract);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Tạo hợp đồng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/Products - Danh sách tất cả sản phẩm
        public IActionResult Products(int page = 1, int pageSize = 10)
        {
            // Đếm tổng số sản phẩm
            var totalItems = _db.Products.Count();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Lấy sản phẩm với pagination
            var products = _db.Products
                             .AsNoTracking()
                             .Include(p => p.Category)
                             .Include(p => p.ProductSizes)
                             .OrderBy(p => p.CategoryID)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveMenu = "Products";
            return View("Products", products);
        }

        // GET /Admin/ViewProduct/{id} - Xem chi tiết sản phẩm
        public IActionResult ViewProduct(int id, string? returnUrl = null)
        {
            var product = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return NotFound();

            ViewBag.ActiveMenu = "Products";
            ViewBag.ReturnUrl = returnUrl;
            return View(product);
        }

        // GET /Admin/Customers - Danh sách tất cả khách hàng
        public IActionResult Customers()
        {
            var customers = _db.Customers
                              .AsNoTracking()
                              .OrderBy(c => c.CustomerID)
                              .ToList();

            // Lấy tất cả orders để tính toán
            var allOrders = _db.Orders.AsNoTracking().ToList();

            // Tạo Dictionary để map CustomerID với stats
            var customerStatsDict = new Dictionary<int, Tuple<int, decimal>>();

            foreach (var customer in customers)
            {
                var orders = allOrders.Where(o => o.CustomerID == customer.CustomerID).ToList();
                var orderCount = orders.Count;
                var totalSpent = orders
                    .Where(o => o.Status == "Đã giao")
                    .Sum(o => o.Total);

                customerStatsDict[customer.CustomerID] = Tuple.Create(orderCount, totalSpent);
            }

            ViewBag.CustomerStats = customerStatsDict;
            ViewBag.ActiveMenu = "Customers";
            return View(customers);
        }

        // GET /Admin/ViewCustomer/{id} - Xem chi tiết khách hàng
        public IActionResult ViewCustomer(int id, int page = 1, int pageSize = 10)
        {
            var customer = _db.Customers
                             .AsNoTracking()
                             .SingleOrDefault(c => c.CustomerID == id);

            if (customer == null) return NotFound();

            // Đếm tổng số đơn hàng
            var totalItems = _db.Orders
                .Where(o => o.CustomerID == id)
                .Count();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Lấy lịch sử đơn hàng với pagination
            var orders = _db.Orders
                           .AsNoTracking()
                           .Include(o => o.Branch)
                           .Include(o => o.OrderDetails)
                           .Where(o => o.CustomerID == id)
                           .OrderByDescending(o => o.CreatedAt)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();

            ViewBag.Orders = orders;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveMenu = "Customers";
            return View(customer);
        }

        // GET /Admin/Reports - Báo cáo & Thống kê
        public IActionResult Reports()
        {
            // Lấy tất cả orders để tính toán
            var allOrders = _db.Orders.AsNoTracking().ToList();

            // Thống kê doanh thu theo tháng (6 tháng gần nhất)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var monthlyRevenue = last6Months.Select(month => new
            {
                Month = month.ToString("MK/yyyy"),
                Revenue = allOrders
                    .Where(o => o.CreatedAt.Month == month.Month &&
                               o.CreatedAt.Year == month.Year &&
                               o.Status == "Đã giao")
                    .Sum(o => o.Total),
                OrderCount = allOrders
                    .Count(o => o.CreatedAt.Month == month.Month &&
                               o.CreatedAt.Year == month.Year),
                DeliveredOrderCount = allOrders
                    .Count(o => o.CreatedAt.Month == month.Month &&
                               o.CreatedAt.Year == month.Year &&
                               o.Status == "Đã giao")
            }).ToList();

            // Top 5 sản phẩm bán chạy - CHỈ tính từ đơn đã giao
            var deliveredOrderIds = allOrders
                .Where(o => o.Status == "Đã giao")
                .Select(o => o.OrderID)
                .ToList();

            var allOrderDetails = _db.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Where(od => od.Product != null && deliveredOrderIds.Contains(od.OrderID))
                .ToList();

            var topProducts = allOrderDetails
                .GroupBy(od => new { od.ProductID, ProductName = od.Product != null ? od.Product.ProductName : "Không xác định" })
                .Select(g => new
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Total)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            // Thống kê đơn hàng theo trạng thái
            var orderStatusStats = allOrders
                .GroupBy(o => o.Status ?? "Chưa xác định")
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Thống kê đơn hàng theo chi nhánh - hiển thị TẤT CẢ chi nhánh
            var allBranches = _db.Branches
                .AsNoTracking()
                .ToList();

            var ordersWithBranch = _db.Orders
                .AsNoTracking()
                .Include(o => o.Branch)
                .Where(o => o.Branch != null)
                .ToList();

            var branchStats = allBranches
                .Select(branch => new
                {
                    BranchID = branch.BranchID,
                    BranchName = branch.Name ?? "Không xác định",
                    OrderCount = ordersWithBranch
                        .Where(o => o.BranchID == branch.BranchID)
                        .Count(),
                    Revenue = ordersWithBranch
                        .Where(o => o.BranchID == branch.BranchID && o.Status == "Đã giao")
                        .Sum(o => o.Total)
                })
                .OrderByDescending(x => x.OrderCount)
                .ThenBy(x => x.BranchName)
                .ToList();

            // Lấy danh sách đơn hàng đã giao gần đây (10 đơn mới nhất)
            var recentDeliveredOrders = _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Where(o => o.Status == "Đã giao")
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new
                {
                    OrderID = o.OrderID,
                    OrderCode = o.OrderCode,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Không xác định",
                    BranchName = o.Branch != null ? o.Branch.Name : "Không xác định",
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status
                })
                .ToList();

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.TopProducts = topProducts;
            ViewBag.OrderStatusStats = orderStatusStats;
            ViewBag.BranchStats = branchStats;
            ViewBag.RecentDeliveredOrders = recentDeliveredOrders;
            ViewBag.ActiveMenu = "Reports";
            return View();
        }

        // GET /Admin/Orders - Quản lý đơn hàng cho admin
        public IActionResult Orders(string? status, string? search, int page = 1, int pageSize = 20)
        {
            // Tạo query base để filter và đếm (không Include để tối ưu)
            var baseQuery = _db.Orders.AsNoTracking().AsQueryable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
            {
                baseQuery = baseQuery.Where(o => o.Status == status);
            }

            // Tìm kiếm - TÁCH RIÊNG LOGIC CHO TỪNG LOẠI
            if (!string.IsNullOrEmpty(search))
            {
                var searchTrimmed = search.Trim();
                var searchLower = searchTrimmed.ToLower();
                var searchHasSpaces = searchTrimmed.Contains(" ");

                List<int> customerIdsWithName = new List<int>();
                List<int> orderIdsMatchingSearch = new List<int>();

                // PHÂN TÍCH: Search có khoảng trắng = TÌM THEO TÊN (không tìm mã đơn/SĐT)
                // Search không có khoảng = CÓ THỂ LÀ mã đơn, SĐT, hoặc tên không khoảng
                if (searchHasSpaces)
                {
                    // CHỈ tìm kiếm theo TÊN (Customer.Name và ReceiverName) - EXACT MATCH
                    // Không tìm trong OrderCode và Phone để tránh match nhầm

                    customerIdsWithName = _db.Customers
                        .AsEnumerable()
                        .Where(c =>
                        {
                            if (c.Name == null || string.IsNullOrWhiteSpace(c.Name)) return false;

                            // Normalize: trim và normalize khoảng trắng (nhiều space thành 1 space)
                            var nameNormalized = System.Text.RegularExpressions.Regex.Replace(
                                c.Name.Trim(),
                                @"\s+",
                                " "
                            ).ToLower();

                            var searchNormalized = System.Text.RegularExpressions.Regex.Replace(
                                searchTrimmed,
                                @"\s+",
                                " "
                            ).ToLower();

                            var nameHasSpaces = nameNormalized.Contains(" ");

                            // Search có khoảng: CHỈ match với tên có khoảng
                            if (!nameHasSpaces) return false;
                            // CHỈ EXACT MATCH sau khi normalize
                            return nameNormalized.Equals(searchNormalized, StringComparison.OrdinalIgnoreCase);
                        })
                        .Select(c => c.CustomerID)
                        .ToList();

                    // Tìm trong ReceiverName - EXACT MATCH
                    var orderIdsByName = _db.Orders
                        .AsEnumerable()
                        .Where(o =>
                        {
                            if (o.ReceiverName == null || string.IsNullOrWhiteSpace(o.ReceiverName)) return false;

                            // Normalize: trim và normalize khoảng trắng
                            var receiverNameNormalized = System.Text.RegularExpressions.Regex.Replace(
                                o.ReceiverName.Trim(),
                                @"\s+",
                                " "
                            ).ToLower();

                            var searchNormalized = System.Text.RegularExpressions.Regex.Replace(
                                searchTrimmed,
                                @"\s+",
                                " "
                            ).ToLower();

                            var receiverNameHasSpaces = receiverNameNormalized.Contains(" ");

                            // Search có khoảng: tên phải có khoảng và EXACT MATCH sau khi normalize
                            if (!receiverNameHasSpaces) return false;
                            return receiverNameNormalized.Equals(searchNormalized, StringComparison.OrdinalIgnoreCase);
                        })
                        .Select(o => o.OrderID)
                        .ToList();

                    orderIdsMatchingSearch = orderIdsByName;
                }
                else
                {
                    // Search không có khoảng: CÓ THỂ LÀ mã đơn, SĐT, hoặc tên không khoảng
                    // Tìm trong TẤT CẢ các trường nhưng với logic strict

                    // 1. Tìm theo OrderCode (mã đơn) - exact match hoặc contains
                    var orderIdsByCode = _db.Orders
                        .AsEnumerable()
                        .Where(o => o.OrderCode != null &&
                            o.OrderCode.ToLower().Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                        .Select(o => o.OrderID)
                        .ToList();

                    // 2. Tìm theo ReceiverPhone (SĐT) - exact match hoặc contains
                    var orderIdsByPhone = _db.Orders
                        .AsEnumerable()
                        .Where(o => o.ReceiverPhone != null &&
                            o.ReceiverPhone.Contains(searchTrimmed, StringComparison.OrdinalIgnoreCase))
                        .Select(o => o.OrderID)
                        .ToList();

                    // 3. Tìm theo Customer.Name (CHỈ match tên không có khoảng)
                    customerIdsWithName = _db.Customers
                        .AsEnumerable()
                        .Where(c =>
                        {
                            if (c.Name == null || string.IsNullOrWhiteSpace(c.Name)) return false;

                            var nameLower = c.Name.ToLower().Trim();
                            var nameHasSpaces = nameLower.Contains(" ");

                            // Search không có khoảng: CHỈ match với tên không có khoảng (exact match)
                            if (nameHasSpaces) return false;
                            return nameLower.Equals(searchLower, StringComparison.OrdinalIgnoreCase);
                        })
                        .Select(c => c.CustomerID)
                        .ToList();

                    // 4. Tìm theo ReceiverName (CHỈ match tên không có khoảng)
                    var orderIdsByReceiverName = _db.Orders
                        .AsEnumerable()
                        .Where(o =>
                        {
                            if (o.ReceiverName == null || string.IsNullOrWhiteSpace(o.ReceiverName)) return false;

                            var receiverNameLower = o.ReceiverName.ToLower().Trim();
                            var receiverNameHasSpaces = receiverNameLower.Contains(" ");

                            // Search không có khoảng: CHỈ match với tên không có khoảng
                            if (receiverNameHasSpaces) return false;
                            return receiverNameLower.Equals(searchLower, StringComparison.OrdinalIgnoreCase);
                        })
                        .Select(o => o.OrderID)
                        .ToList();

                    // Gộp tất cả order IDs lại
                    orderIdsMatchingSearch = orderIdsByCode
                        .Union(orderIdsByPhone)
                        .Union(orderIdsByReceiverName)
                        .ToList();
                }

                // Filter baseQuery
                baseQuery = baseQuery.Where(o =>
                    orderIdsMatchingSearch.Contains(o.OrderID) ||
                    customerIdsWithName.Contains(o.CustomerID)
                );
            }

            // Đếm tổng số đơn hàng sau khi filter (trước khi Include)
            var totalCount = baseQuery.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Validate page
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lấy danh sách order IDs sau khi filter và phân trang
            var orderIds = baseQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => o.OrderID)
                .ToList();

            // Load orders với Include (chỉ load những order cần thiết)
            var orders = _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Where(o => orderIds.Contains(o.OrderID))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            // Lấy danh sách trạng thái duy nhất
            var statuses = _db.Orders
                .Select(o => o.Status)
                .Distinct()
                .Where(s => s != null)
                .OrderBy(s => s)
                .ToList();

            ViewBag.Statuses = statuses;
            ViewBag.CurrentStatus = status ?? "Tất cả";
            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveMenu = "Orders";
            return View(orders);
        }

        // GET /Admin/ViewOrder/{id} - Xem chi tiết đơn hàng
        public IActionResult ViewOrder(int id, string? status = null, string? search = null, int page = 1)
        {
            var order = _db.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductSize)
                .SingleOrDefault(o => o.OrderID == id);

            if (order == null) return NotFound();

            // Lưu các filter params để quay lại
            ViewBag.ActiveMenu = "Orders";
            ViewBag.ReturnStatus = status;
            ViewBag.ReturnSearch = search;
            ViewBag.ReturnPage = page;
            return View(order);
        }

        // POST /Admin/UpdateOrderStatus - Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });

            // Validate trạng thái
            var validStatuses = new[] { "Chờ xác nhận", "Đã xác nhận", "Đang giao", "Đã giao", "Đã hủy", "Chờ hoàn tiền", "Đã hoàn tiền" };
            if (!validStatuses.Contains(status))
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });

            order.Status = status;

            // Nếu hủy đơn, lưu thời gian hủy
            if (status == "Đã hủy")
            {
                order.CancelledAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
        }

        // GET /Admin/Categories - Quản lý danh mục sản phẩm
        public IActionResult Categories()
        {
            var categories = _db.ProductCategories
                .AsNoTracking()
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToList();

            ViewBag.ActiveMenu = "Categories";
            return View(categories);
        }

        // GET /Admin/ViewCategory/{id} - Xem chi tiết danh mục
        public IActionResult ViewCategory(int id)
        {
            var category = _db.ProductCategories
                .AsNoTracking()
                .Include(c => c.Products)
                    .ThenInclude(p => p.ProductSizes)
                .FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            ViewBag.ActiveMenu = "Categories";
            return View(category);
        }

        // ========== QUẢN LÝ CHI NHÁNH ==========

        // GET /Admin/Branches - Danh sách tất cả chi nhánh
        public async Task<IActionResult> Branches(int page = 1, int pageSize = 10, int? regionId = null)
        {
            // Query base
            var query = _db.Branches.AsNoTracking().Include(b => b.Orders).Include(b => b.Region).AsQueryable();

            // Filter theo region nếu có
            if (regionId.HasValue && regionId.Value > 0)
            {
                query = query.Where(b => b.RegionID == regionId.Value);
            }

            // Đếm tổng số chi nhánh sau khi filter
            var totalItems = query.Count();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Lấy chi nhánh với pagination
            var branches = query
                             .OrderBy(b => b.Name)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            // Tính số nhân viên cho mỗi branch
            var branchEmployeeCounts = new Dictionary<int, int>();
            foreach (var branch in branches)
            {
                branchEmployeeCounts[branch.BranchID] = await _db.Employees.CountAsync(e => e.BranchID == branch.BranchID);
            }
            ViewBag.BranchEmployeeCounts = branchEmployeeCounts;

            // Lấy tất cả regions để hiển thị
            var regions = _db.Regions
                            .AsNoTracking()
                            .OrderBy(r => r.RegionName)
                            .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Regions = regions;
            ViewBag.CurrentRegionId = regionId;
            ViewBag.ActiveMenu = "Branches";
            return View(branches);
        }

        // GET /Admin/AddBranch - Form thêm chi nhánh
        public IActionResult AddBranch()
        {
            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionName)
                             .ToList();
            ViewBag.Regions = regions;
            return PartialView("_AddEditBranchModal", new Branch());
        }

        // POST /Admin/AddBranch - Thêm chi nhánh mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBranch([FromForm] string Name, [FromForm] string? Address, [FromForm] string? Phone, [FromForm] int RegionID, [FromForm] string? City, [FromForm] decimal Latitude, [FromForm] decimal Longitude)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentEmpId))
                    return Json(new { success = false, message = "Bạn cần đăng nhập" });

                // Validate
                if (string.IsNullOrWhiteSpace(Name))
                    return Json(new { success = false, message = "Tên chi nhánh không được để trống" });

                if (RegionID <= 0)
                    return Json(new { success = false, message = "Vui lòng chọn khu vực" });

                // Kiểm tra Region có tồn tại không
                var regionExists = await _db.Regions.AnyAsync(r => r.RegionID == RegionID);
                if (!regionExists)
                    return Json(new { success = false, message = "Khu vực không tồn tại" });

                // Kiểm tra tên branch không trùng trong cùng region
                var existingBranch = await _db.Branches
                    .FirstOrDefaultAsync(b => b.Name.Trim() == Name.Trim() && b.RegionID == RegionID);

                if (existingBranch != null)
                {
                    return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{Name.Trim()}\" trong khu vực này" });
                }

                // Tạo branch mới
                var branch = new Branch
                {
                    Name = Name.Trim(),
                    Address = Address?.Trim(),
                    Phone = Phone?.Trim(),
                    RegionID = RegionID,
                    City = City?.Trim(),
                    Latitude = Latitude,
                    Longitude = Longitude
                };

                _db.Branches.Add(branch);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm chi nhánh thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/EditBranch/{id} - Form sửa chi nhánh
        public IActionResult EditBranch(int id)
        {
            var branch = _db.Branches
                           .AsNoTracking()
                           .FirstOrDefault(b => b.BranchID == id);

            if (branch == null)
                return Json(new { success = false, message = "Chi nhánh không tồn tại" });

            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionName)
                             .ToList();
            ViewBag.Regions = regions;
            return PartialView("_AddEditBranchModal", branch);
        }

        // POST /Admin/EditBranch/{id} - Cập nhật chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranch(int id, [FromForm] string Name, [FromForm] string? Address, [FromForm] string? Phone, [FromForm] int RegionID, [FromForm] string? City, [FromForm] decimal Latitude, [FromForm] decimal Longitude)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var existingBranch = await _db.Branches
                                          .FirstOrDefaultAsync(b => b.BranchID == id);

            if (existingBranch == null)
                return Json(new { success = false, message = "Chi nhánh không tồn tại" });

            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(Name))
                    return Json(new { success = false, message = "Tên chi nhánh không được để trống" });

                if (RegionID <= 0)
                    return Json(new { success = false, message = "Vui lòng chọn khu vực" });

                // Kiểm tra Region có tồn tại không
                var regionExists = await _db.Regions.AnyAsync(r => r.RegionID == RegionID);
                if (!regionExists)
                    return Json(new { success = false, message = "Khu vực không tồn tại" });

                // Kiểm tra tên branch không trùng với branch khác trong cùng region
                var duplicateBranch = await _db.Branches
                    .FirstOrDefaultAsync(b => b.Name.Trim() == Name.Trim() &&
                                             b.RegionID == RegionID &&
                                             b.BranchID != id);

                if (duplicateBranch != null)
                {
                    return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{Name.Trim()}\" trong khu vực này" });
                }

                // Cập nhật thông tin
                existingBranch.Name = Name.Trim();
                existingBranch.Address = Address?.Trim();
                existingBranch.Phone = Phone?.Trim();
                existingBranch.RegionID = RegionID;
                existingBranch.City = City?.Trim();
                existingBranch.Latitude = Latitude;
                existingBranch.Longitude = Longitude;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật chi nhánh thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/DeleteBranch/{id} - Xóa chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var branch = await _db.Branches
                                  .FirstOrDefaultAsync(b => b.BranchID == id);

            if (branch == null)
                return Json(new { success = false, message = "Chi nhánh không tồn tại" });

            try
            {
                // Đếm số nhân viên và đơn hàng
                var employeeCount = await _db.Employees.CountAsync(e => e.BranchID == id);
                var orderCount = await _db.Orders.CountAsync(o => o.BranchID == id);

                // Kiểm tra xem chi nhánh có đơn hàng không
                if (orderCount > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa chi nhánh vì đang có {orderCount} đơn hàng liên quan. Vui lòng xử lý đơn hàng trước khi xóa."
                    });
                }

                // Kiểm tra xem có nhân viên nào thuộc chi nhánh này không
                if (employeeCount > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa chi nhánh vì đang có {employeeCount} nhân viên thuộc chi nhánh này. Vui lòng chuyển nhân viên trước khi xóa."
                    });
                }

                // Xóa chi nhánh
                _db.Branches.Remove(branch);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa chi nhánh thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ MÃ GIẢM GIÁ (DISCOUNTS) - CHỈ XEM ==========

        // GET /Admin/Discounts - Danh sách mã giảm giá (chỉ xem, không CRUD)
        public IActionResult Discounts(string? filter, int page = 1, int pageSize = 10)
        {
            var query = _db.Discounts.AsNoTracking().AsQueryable();

            // Lọc theo loại (Sale hoặc Ship)
            if (!string.IsNullOrEmpty(filter) && filter != "Tất cả")
            {
                if (filter == "Sale")
                {
                    // Sale: Percentage và FixedAmount
                    query = query.Where(d => d.Type == DiscountType.Percentage || d.Type == DiscountType.FixedAmount);
                }
                else if (filter == "Ship")
                {
                    // Ship: FreeShipping, FixedShippingDiscount, PercentShippingDiscount
                    query = query.Where(d => d.Type == DiscountType.FreeShipping ||
                                            d.Type == DiscountType.FixedShippingDiscount ||
                                            d.Type == DiscountType.PercentShippingDiscount);
                }
            }

            // Đếm tổng số sau khi filter
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Lấy dữ liệu với pagination
            var discounts = query
                             .OrderByDescending(d => d.Id)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentFilter = filter ?? "Tất cả";
            ViewBag.ActiveMenu = "Discounts";
            return View(discounts);
        }

        // ========== QUẢN LÝ TIN TỨC (NEWS) ==========

        // GET /Admin/News - Danh sách tin tức
        public IActionResult News(int page = 1, int pageSize = 10)
        {
            var totalItems = _db.News.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var news = _db.News
                        .AsNoTracking()
                        .Include(n => n.Discount)
                        .OrderByDescending(n => n.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.ActiveMenu = "News";
            return View(news);
        }

        // News chỉ xem, không có CRUD - CRUD thông qua NewsRequest

        // ========== XUẤT BÁO CÁO EXCEL ==========

        // GET /Admin/ExportReports - Xuất báo cáo Excel
        [HttpGet]
        public IActionResult ExportReports(string? reportType, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Báo cáo đơn hàng");

                    // ===================== TIÊU ĐỀ CHÍNH =====================
                    ws.Cell("A1").Value = "BÁO CÁO ĐƠN HÀNG";
                    ws.Range("A1:F1").Merge();
                    ws.Cell("A1").Style
                        .Font.SetBold()
                        .Font.SetFontSize(18)
                        .Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromArgb(153, 102, 51))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                    // ===================== TIÊU ĐỀ CỘT =====================
                    string[] headers = { "Mã đơn", "Khách hàng", "Chi nhánh", "Tổng tiền", "Ngày tạo", "Trạng thái" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(3, i + 1);
                        cell.Value = headers[i];
                        cell.Style
                            .Font.SetBold()
                            .Fill.SetBackgroundColor(XLColor.Beige)
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    }

                    // ===================== LẤY DỮ LIỆU =====================
                    var orders = _db.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.Branch)
                        .Where(o => o.Status == "Đã giao")
                        .AsQueryable();

                    if (startDate.HasValue)
                        orders = orders.Where(o => o.CreatedAt >= startDate.Value);
                    if (endDate.HasValue)
                        orders = orders.Where(o => o.CreatedAt <= endDate.Value.AddDays(1));

                    var data = orders.OrderByDescending(o => o.CreatedAt).ToList();

                    // ===================== GHI DỮ LIỆU =====================
                    int row = 4;
                    foreach (var order in data)
                    {
                        ws.Cell(row, 1).Value = order.OrderCode ?? order.OrderID.ToString();
                        ws.Cell(row, 2).Value = order.Customer?.Name ?? "N/A";
                        ws.Cell(row, 3).Value = order.Branch?.Name ?? "N/A";
                        ws.Cell(row, 4).Value = order.Total;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0 ₫";
                        ws.Cell(row, 5).Value = order.CreatedAt.ToString("dd/MK/yyyy HH:mm");
                        ws.Cell(row, 6).Value = order.Status ?? "N/A";

                        ws.Range(row, 1, row, 6)
                            .Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                        row++;
                    }

                    // ===================== XỬ LÝ TRƯỜNG HỢP RỖNG =====================
                    if (data.Count == 0)
                    {
                        ws.Cell("A4").Value = "Không có dữ liệu trong khoảng thời gian đã chọn.";
                        ws.Range("A4:F4").Merge();
                        ws.Cell("A4").Style
                            .Font.SetItalic()
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }

                    // ===================== FORMAT + XUẤT FILE =====================
                    ws.Columns().AdjustToContents();
                    ws.SheetView.FreezeRows(3); // Cố định tiêu đề khi cuộn

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var fileName = $"BaoCao_{reportType ?? "DonHang"}_{DateTime.Now:yyyyMKdd_HHmmss}.xlsx";

                        return File(content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất báo cáo: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        // ========== QUẢN LÝ DUYỆT YÊU CẦU (APPROVALS) ==========

        // GET /Admin/Approvals - Danh sách yêu cầu cần duyệt
        public IActionResult Approvals(string? status, string? requestType, int page = 1, int pageSize = 15)
        {
            // Mặc định hiển thị cả Discount và News requests
            requestType = requestType ?? "all";

            // Query DiscountRequests
            var discountQuery = _db.DiscountRequests
                .AsNoTracking()
                .Include(dr => dr.RequestedByEmployee)
                .Include(dr => dr.ReviewedByEmployee)
                .Include(dr => dr.Discount)
                .AsQueryable();

            // Query NewsRequests
            var newsQuery = _db.NewsRequests
                .AsNoTracking()
                .Include(nr => nr.RequestedByEmployee)
                .Include(nr => nr.ReviewedByEmployee)
                .Include(nr => nr.News)
                .Include(nr => nr.Discount)
                .AsQueryable();

            // Query EmployeeBranchRequests
            var employeeBranchQuery = _db.EmployeeBranchRequests
                .AsNoTracking()
                .Include(ebr => ebr.RequestedByEmployee)
                .Include(ebr => ebr.ReviewedByEmployee)
                .Include(ebr => ebr.Employee)
                    .ThenInclude(e => e.Branch)  // Load chi nhánh hiện tại của nhân viên
                .Include(ebr => ebr.Branch)  // Load chi nhánh đích
                .AsQueryable();

            // Query ProductRequests
            var productQuery = _db.ProductRequests
                .AsNoTracking()
                .Include(pr => pr.RequestedByEmployee)
                .Include(pr => pr.ReviewedByEmployee)
                .Include(pr => pr.Product)
                .Include(pr => pr.Category)
                .AsQueryable();

            // Query CategoryRequests
            var categoryQuery = _db.CategoryRequests
                .AsNoTracking()
                .Include(cr => cr.RequestedByEmployee)
                .Include(cr => cr.ReviewedByEmployee)
                .Include(cr => cr.Category)
                .AsQueryable();

            // Query BranchRequests
            var branchQuery = _db.BranchRequests
                .AsNoTracking()
                .Include(br => br.RequestedByEmployee)
                .Include(br => br.ReviewedByEmployee)
                .Include(br => br.Branch)
                .Include(br => br.Region)
                .AsQueryable();

            // Tính tổng số theo trạng thái từ TẤT CẢ các request (không phụ thuộc vào filter)
            var totalPending = _db.DiscountRequests.Count(dr => dr.Status == RequestStatus.Pending)
                + _db.NewsRequests.Count(nr => nr.Status == RequestStatus.Pending)
                + _db.EmployeeBranchRequests.Count(ebr => ebr.Status == RequestStatus.Pending)
                + _db.ProductRequests.Count(pr => pr.Status == RequestStatus.Pending)
                + _db.CategoryRequests.Count(cr => cr.Status == RequestStatus.Pending)
                + _db.BranchRequests.Count(br => br.Status == RequestStatus.Pending);

            var totalApproved = _db.DiscountRequests.Count(dr => dr.Status == RequestStatus.Approved)
                + _db.NewsRequests.Count(nr => nr.Status == RequestStatus.Approved)
                + _db.EmployeeBranchRequests.Count(ebr => ebr.Status == RequestStatus.Approved)
                + _db.ProductRequests.Count(pr => pr.Status == RequestStatus.Approved)
                + _db.CategoryRequests.Count(cr => cr.Status == RequestStatus.Approved)
                + _db.BranchRequests.Count(br => br.Status == RequestStatus.Approved);

            var totalRejected = _db.DiscountRequests.Count(dr => dr.Status == RequestStatus.Rejected)
                + _db.NewsRequests.Count(nr => nr.Status == RequestStatus.Rejected)
                + _db.EmployeeBranchRequests.Count(ebr => ebr.Status == RequestStatus.Rejected)
                + _db.ProductRequests.Count(pr => pr.Status == RequestStatus.Rejected)
                + _db.CategoryRequests.Count(cr => cr.Status == RequestStatus.Rejected)
                + _db.BranchRequests.Count(br => br.Status == RequestStatus.Rejected);

            // Lọc theo loại request
            if (requestType == "discount")
            {
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
                employeeBranchQuery = employeeBranchQuery.Where(ebr => false); // Không hiển thị EmployeeBranch
                productQuery = productQuery.Where(pr => false); // Không hiển thị Product
                categoryQuery = categoryQuery.Where(cr => false); // Không hiển thị Category
                branchQuery = branchQuery.Where(br => false); // Không hiển thị Branch
            }
            else if (requestType == "news")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
                employeeBranchQuery = employeeBranchQuery.Where(ebr => false); // Không hiển thị EmployeeBranch
                productQuery = productQuery.Where(pr => false); // Không hiển thị Product
                categoryQuery = categoryQuery.Where(cr => false); // Không hiển thị Category
                branchQuery = branchQuery.Where(br => false); // Không hiển thị Branch
            }
            else if (requestType == "employeebranch")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
                productQuery = productQuery.Where(pr => false); // Không hiển thị Product
                categoryQuery = categoryQuery.Where(cr => false); // Không hiển thị Category
                branchQuery = branchQuery.Where(br => false); // Không hiển thị Branch
            }
            else if (requestType == "product")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
                employeeBranchQuery = employeeBranchQuery.Where(ebr => false); // Không hiển thị EmployeeBranch
                categoryQuery = categoryQuery.Where(cr => false); // Không hiển thị Category
                branchQuery = branchQuery.Where(br => false); // Không hiển thị Branch
            }
            else if (requestType == "category")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
                employeeBranchQuery = employeeBranchQuery.Where(ebr => false); // Không hiển thị EmployeeBranch
                productQuery = productQuery.Where(pr => false); // Không hiển thị Product
                branchQuery = branchQuery.Where(br => false); // Không hiển thị Branch
            }
            else if (requestType == "branch")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
                employeeBranchQuery = employeeBranchQuery.Where(ebr => false); // Không hiển thị EmployeeBranch
                productQuery = productQuery.Where(pr => false); // Không hiển thị Product
                categoryQuery = categoryQuery.Where(cr => false); // Không hiển thị Category
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    discountQuery = discountQuery.Where(dr => dr.Status == statusEnum);
                    newsQuery = newsQuery.Where(nr => nr.Status == statusEnum);
                    employeeBranchQuery = employeeBranchQuery.Where(ebr => ebr.Status == statusEnum);
                    productQuery = productQuery.Where(pr => pr.Status == statusEnum);
                    categoryQuery = categoryQuery.Where(cr => cr.Status == statusEnum);
                    branchQuery = branchQuery.Where(br => br.Status == statusEnum);
                }
            }
            else
            {
                // Mặc định chỉ hiển thị Pending
                discountQuery = discountQuery.Where(dr => dr.Status == RequestStatus.Pending);
                newsQuery = newsQuery.Where(nr => nr.Status == RequestStatus.Pending);
                employeeBranchQuery = employeeBranchQuery.Where(ebr => ebr.Status == RequestStatus.Pending);
                productQuery = productQuery.Where(pr => pr.Status == RequestStatus.Pending);
                categoryQuery = categoryQuery.Where(cr => cr.Status == RequestStatus.Pending);
                branchQuery = branchQuery.Where(br => br.Status == RequestStatus.Pending);
            }

            // Đếm tổng số yêu cầu
            var discountCount = discountQuery.Count();
            var newsCount = newsQuery.Count();
            var employeeBranchCount = employeeBranchQuery.Count();
            var productCount = productQuery.Count();
            var categoryCount = categoryQuery.Count();
            var branchCount = branchQuery.Count();
            var totalCount = discountCount + newsCount + employeeBranchCount + productCount + categoryCount + branchCount;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Lấy danh sách DiscountRequests
            var discountRequests = discountQuery
                .OrderBy(dr => dr.Status)        // Sắp xếp theo Status (Pending, Approved, Rejected)
                .ThenByDescending(dr => dr.RequestedAt)  // Sau đó theo thời gian (mới nhất trước)
                .ToList();

            // Lấy danh sách NewsRequests
            var newsRequests = newsQuery
                .OrderBy(nr => nr.Status)        // Sắp xếp theo Status
                .ThenByDescending(nr => nr.RequestedAt)  // Sau đó theo thời gian
                .ToList();

            // Lấy danh sách EmployeeBranchRequests
            var employeeBranchRequests = employeeBranchQuery
                .OrderBy(ebr => ebr.Status)        // Sắp xếp theo Status
                .ThenByDescending(ebr => ebr.RequestedAt)  // Sau đó theo thời gian
                .ToList();

            // Lấy danh sách ProductRequests
            var productRequests = productQuery
                .OrderBy(pr => pr.Status)        // Sắp xếp theo Status
                .ThenByDescending(pr => pr.RequestedAt)  // Sau đó theo thời gian
                .ToList();

            // Lấy danh sách CategoryRequests
            var categoryRequests = categoryQuery
                .OrderBy(cr => cr.Status)        // Sắp xếp theo Status
                .ThenByDescending(cr => cr.RequestedAt)  // Sau đó theo thời gian
                .ToList();

            // Lấy danh sách BranchRequests
            var branchRequests = branchQuery
                .OrderBy(br => br.Status)        // Sắp xếp theo Status
                .ThenByDescending(br => br.RequestedAt)  // Sau đó theo thời gian
                .ToList();

            // Kết hợp và phân trang - Sử dụng Dictionary để tương thích với dynamic
            var allRequestsList = new List<Dictionary<string, object>>();

            foreach (var dr in discountRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "Discount" },
                    { "Request", dr },
                    { "RequestedAt", dr.RequestedAt },
                    { "RequestType", dr.RequestType },
                    { "Status", dr.Status }
                });
            }

            foreach (var nr in newsRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "News" },
                    { "Request", nr },
                    { "RequestedAt", nr.RequestedAt },
                    { "RequestType", nr.RequestType },
                    { "Status", nr.Status }
                });
            }

            foreach (var ebr in employeeBranchRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "EmployeeBranch" },
                    { "Request", ebr },
                    { "RequestedAt", ebr.RequestedAt },
                    { "RequestType", ebr.RequestType },
                    { "Status", ebr.Status }
                });
            }

            foreach (var pr in productRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "Product" },
                    { "Request", pr },
                    { "RequestedAt", pr.RequestedAt },
                    { "RequestType", pr.RequestType },
                    { "Status", pr.Status }
                });
            }

            foreach (var cr in categoryRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "Category" },
                    { "Request", cr },
                    { "RequestedAt", cr.RequestedAt },
                    { "RequestType", cr.RequestType },
                    { "Status", cr.Status }
                });
            }

            foreach (var br in branchRequests)
            {
                allRequestsList.Add(new Dictionary<string, object>
                {
                    { "Type", "Branch" },
                    { "Request", br },
                    { "RequestedAt", br.RequestedAt },
                    { "RequestType", br.RequestType },
                    { "Status", br.Status }
                });
            }

            // Sắp xếp: Status trước, sau đó RequestedAt
            var allRequests = allRequestsList
                .OrderBy(x => (RequestStatus)x["Status"])      // Sắp xếp theo Status (Pending=0, Approved=1, Rejected=2)
                .ThenByDescending(x => (DateTime)x["RequestedAt"])  // Sau đó theo thời gian (mới nhất trước)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentStatus = status ?? "Pending";
            ViewBag.CurrentRequestType = requestType;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPending = totalPending;
            ViewBag.TotalApproved = totalApproved;
            ViewBag.TotalRejected = totalRejected;
            ViewBag.ActiveMenu = "Approvals";
            return View(allRequests);
        }

        // GET /Admin/ViewApproval/{id} - Xem chi tiết yêu cầu
        public IActionResult ViewApproval(int id, string? type)
        {
            if (type == "news")
            {
                var request = _db.NewsRequests
                    .AsNoTracking()
                    .Include(nr => nr.RequestedByEmployee)
                    .Include(nr => nr.ReviewedByEmployee)
                    .Include(nr => nr.News)
                    .Include(nr => nr.Discount)
                    .FirstOrDefault(nr => nr.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "News";
                ViewBag.ActiveMenu = "Approvals";
                return View("ViewNewsApproval", request);
            }
            else if (type == "employeebranch")
            {
                var request = _db.EmployeeBranchRequests
                    .AsNoTracking()
                    .Include(ebr => ebr.RequestedByEmployee)
                    .Include(ebr => ebr.ReviewedByEmployee)
                    .Include(ebr => ebr.Employee)
                        .ThenInclude(e => e.Branch)  // Load chi nhánh hiện tại của nhân viên
                    .Include(ebr => ebr.Branch)  // Load chi nhánh đích
                    .FirstOrDefault(ebr => ebr.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "EmployeeBranch";
                ViewBag.ActiveMenu = "Approvals";
                return View("ViewEmployeeBranchApproval", request);
            }
            else if (type == "product")
            {
                var request = _db.ProductRequests
                    .AsNoTracking()
                    .Include(pr => pr.RequestedByEmployee)
                    .Include(pr => pr.ReviewedByEmployee)
                    .Include(pr => pr.Product)
                    .Include(pr => pr.Category)
                    .FirstOrDefault(pr => pr.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "Product";
                ViewBag.ActiveMenu = "Approvals";
                return View("ViewProductApproval", request);
            }
            else if (type == "category")
            {
                var request = _db.CategoryRequests
                    .AsNoTracking()
                    .Include(cr => cr.RequestedByEmployee)
                    .Include(cr => cr.ReviewedByEmployee)
                    .Include(cr => cr.Category)
                    .FirstOrDefault(cr => cr.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "Category";
                ViewBag.ActiveMenu = "Approvals";
                return View("ViewCategoryApproval", request);
            }
            else if (type == "branch")
            {
                var request = _db.BranchRequests
                    .AsNoTracking()
                    .Include(br => br.RequestedByEmployee)
                    .Include(br => br.ReviewedByEmployee)
                    .Include(br => br.Branch)
                    .Include(br => br.Region)
                    .FirstOrDefault(br => br.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "Branch";
                ViewBag.ActiveMenu = "Approvals";
                return View("ViewBranchApproval", request);
            }
            else
            {
                var request = _db.DiscountRequests
                    .AsNoTracking()
                    .Include(dr => dr.RequestedByEmployee)
                    .Include(dr => dr.ReviewedByEmployee)
                    .Include(dr => dr.Discount)
                    .FirstOrDefault(dr => dr.Id == id);

                if (request == null)
                    return NotFound();

                ViewBag.RequestType = "Discount";
                ViewBag.ActiveMenu = "Approvals";
                return View(request);
            }
        }

        // POST /Admin/ApproveDiscountRequest/{id} - Duyệt yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDiscountRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.DiscountRequests
                .Include(dr => dr.Discount)
                .FirstOrDefaultAsync(dr => dr.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                // Thực hiện hành động dựa trên RequestType
                if (request.RequestType == RequestType.Add)
                {
                    // Tạo Discount mới
                    var discount = new Discount
                    {
                        Code = request.Code,
                        Type = request.Type,
                        Percent = request.Percent,
                        Amount = request.Amount,
                        StartAt = request.StartAt,
                        EndAt = request.EndAt,
                        IsActive = request.IsActive,
                        UsageLimit = request.UsageLimit
                    };
                    _db.Discounts.Add(discount);
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Sửa Discount
                    if (request.DiscountId.HasValue)
                    {
                        var discount = await _db.Discounts.FindAsync(request.DiscountId.Value);
                        if (discount != null)
                        {
                            discount.Code = request.Code;
                            discount.Type = request.Type;
                            discount.Percent = request.Percent;
                            discount.Amount = request.Amount;
                            discount.StartAt = request.StartAt;
                            discount.EndAt = request.EndAt;
                            discount.IsActive = request.IsActive;
                            discount.UsageLimit = request.UsageLimit;
                        }
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa Discount
                    if (request.DiscountId.HasValue)
                    {
                        var discount = await _db.Discounts.FindAsync(request.DiscountId.Value);
                        if (discount != null)
                        {
                            _db.Discounts.Remove(discount);
                        }
                    }
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectDiscountRequest/{id} - Từ chối yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDiscountRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.DiscountRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Rejected;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;
                request.RejectionReason = rejectionReason?.Trim();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã từ chối yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ DUYỆT YÊU CẦU TIN TỨC ==========

        // POST /Admin/ApproveNewsRequest/{id} - Duyệt yêu cầu tin tức
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveNewsRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.NewsRequests
                .Include(nr => nr.News)
                .FirstOrDefaultAsync(nr => nr.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                // Thực hiện hành động dựa trên RequestType
                if (request.RequestType == RequestType.Add)
                {
                    // Tạo News mới
                    var news = new News
                    {
                        Title = request.Title,
                        Content = request.Content,
                        ImageUrl = request.ImageUrl,
                        DiscountId = request.DiscountId,
                        CreatedAt = request.CreatedAt
                    };
                    _db.News.Add(news);
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Sửa News
                    if (request.NewsId.HasValue)
                    {
                        var news = await _db.News.FindAsync(request.NewsId.Value);
                        if (news != null)
                        {
                            news.Title = request.Title;
                            news.Content = request.Content;
                            news.ImageUrl = request.ImageUrl;
                            news.DiscountId = request.DiscountId;
                        }
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa News
                    if (request.NewsId.HasValue)
                    {
                        var news = await _db.News.FindAsync(request.NewsId.Value);
                        if (news != null)
                        {
                            _db.News.Remove(news);
                        }
                    }
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectNewsRequest/{id} - Từ chối yêu cầu tin tức
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNewsRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.NewsRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Rejected;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;
                request.RejectionReason = rejectionReason?.Trim();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã từ chối yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ DUYỆT YÊU CẦU NHÂN VIÊN - CHI NHÁNH ==========

        // POST /Admin/ApproveEmployeeBranchRequest/{id} - Duyệt yêu cầu thêm nhân viên vào chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEmployeeBranchRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.EmployeeBranchRequests
                .Include(ebr => ebr.Employee)
                .Include(ebr => ebr.Branch)
                .FirstOrDefaultAsync(ebr => ebr.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                // Thực hiện hành động dựa trên RequestType TRƯỚC
                // Chỉ set Status = Approved SAU KHI tất cả hành động thành công
                if (request.RequestType == RequestType.Add)
                {
                    // Thêm nhân viên vào chi nhánh
                    if (!request.BranchId.HasValue)
                    {
                        return Json(new { success = false, message = "Thiếu thông tin chi nhánh" });
                    }

                    // Trường hợp 1: EmployeeId có giá trị - nhân viên đã tồn tại, chỉ cần gán BranchID
                    if (!string.IsNullOrEmpty(request.EmployeeId))
                    {
                        var employee = await _db.Employees.FindAsync(request.EmployeeId);
                        if (employee != null)
                        {
                            employee.BranchID = request.BranchId.Value;
                        }
                        else
                        {
                            return Json(new { success = false, message = "Nhân viên không tồn tại" });
                        }
                    }
                    // Trường hợp 2: EmployeeId = NULL - nhân viên mới chưa tồn tại
                    // Tạo nhân viên mới từ thông tin trong request
                    else
                    {
                        // Kiểm tra thông tin bắt buộc
                        if (string.IsNullOrWhiteSpace(request.FullName))
                        {
                            return Json(new { success = false, message = "Thiếu thông tin tên nhân viên" });
                        }

                        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                        {
                            return Json(new { success = false, message = "Thiếu thông tin số điện thoại" });
                        }

                        if (string.IsNullOrWhiteSpace(request.Email))
                        {
                            return Json(new { success = false, message = "Thiếu thông tin email" });
                        }

                        // Kiểm tra trùng email hoặc số điện thoại
                        if (await _db.Employees.AnyAsync(e => e.Email == request.Email))
                        {
                            return Json(new { success = false, message = "Email đã tồn tại trong hệ thống" });
                        }

                        if (await _db.Employees.AnyAsync(e => e.PhoneNumber == request.PhoneNumber))
                        {
                            return Json(new { success = false, message = "Số điện thoại đã tồn tại trong hệ thống" });
                        }

                        // Tạo EmployeeID mới (E + số thứ tự)
                        var lastEmployee = await _db.Employees
                            .Where(e => e.EmployeeID != null && e.EmployeeID.StartsWith("E"))
                            .OrderByDescending(e => e.EmployeeID)
                            .FirstOrDefaultAsync();

                        string newEmployeeId;
                        if (lastEmployee != null && !string.IsNullOrEmpty(lastEmployee.EmployeeID))
                        {
                            var lastNumber = int.Parse(lastEmployee.EmployeeID.Substring(1));
                            newEmployeeId = $"E{(lastNumber + 1):D3}";
                        }
                        else
                        {
                            newEmployeeId = "E001";
                        }

                        // Tạo nhân viên mới
                        var newEmployee = new Employee
                        {
                            EmployeeID = newEmployeeId,
                            FullName = request.FullName,
                            DateOfBirth = request.DateOfBirth,
                            Gender = request.Gender,
                            PhoneNumber = request.PhoneNumber,
                            Email = request.Email,
                            City = request.City,
                            Nationality = request.Nationality ?? "Việt Nam",
                            Ethnicity = request.Ethnicity ?? "Kinh",
                            EmergencyPhone1 = request.EmergencyPhone1,
                            EmergencyPhone2 = request.EmergencyPhone2,
                            RoleID = request.RoleID ?? "EM", // Mặc định là EM
                            BranchID = request.BranchId.Value,
                            Password = _authService.HashPassword("1234567"), // Mật khẩu mặc định (đã hash)
                            IsHashed = true,
                            HireDate = DateTime.UtcNow,
                            IsActive = true,
                            AvatarUrl = "https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png" // Avatar mặc định
                        };

                        _db.Employees.Add(newEmployee);

                        // Cập nhật EmployeeId trong request để lưu lại
                        request.EmployeeId = newEmployeeId;
                    }
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Chuyển nhân viên sang chi nhánh khác
                    if (!string.IsNullOrEmpty(request.EmployeeId) && request.BranchId.HasValue)
                    {
                        var employee = await _db.Employees.FindAsync(request.EmployeeId);
                        if (employee != null)
                        {
                            employee.BranchID = request.BranchId.Value;
                        }
                        else
                        {
                            return Json(new { success = false, message = "Nhân viên không tồn tại" });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Thiếu thông tin nhân viên hoặc chi nhánh" });
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa nhân viên khỏi chi nhánh (set BranchID = null)
                    if (!string.IsNullOrEmpty(request.EmployeeId))
                    {
                        var employee = await _db.Employees.FindAsync(request.EmployeeId);
                        if (employee != null)
                        {
                            employee.BranchID = null;
                        }
                        else
                        {
                            return Json(new { success = false, message = "Nhân viên không tồn tại" });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Thiếu thông tin nhân viên" });
                    }
                }

                // CHỈ SET STATUS = APPROVED SAU KHI TẤT CẢ HÀNH ĐỘNG THÀNH CÔNG
                // Đảm bảo chỉ approve khi thực sự đã thêm/chuyển/xóa nhân viên thành công
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectEmployeeBranchRequest/{id} - Từ chối yêu cầu thêm nhân viên vào chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEmployeeBranchRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.EmployeeBranchRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Rejected;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;
                request.RejectionReason = rejectionReason?.Trim();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã từ chối yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ DUYỆT YÊU CẦU SẢN PHẨM ==========

        // POST /Admin/ApproveProductRequest/{id} - Duyệt yêu cầu sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProductRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.ProductRequests
                .Include(pr => pr.Product)
                .FirstOrDefaultAsync(pr => pr.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                // Thực hiện hành động dựa trên RequestType
                if (request.RequestType == RequestType.Add)
                {
                    // Tạo Product mới
                    var product = new Product
                    {
                        ProductName = request.ProductName,
                        CategoryID = request.CategoryID,
                        Description = request.Description,
                        Image_Url = request.Image_Url,
                        IsActive = request.IsActive
                    };
                    _db.Products.Add(product);
                    await _db.SaveChangesAsync(); // Lưu để lấy ProductID

                    // Thêm ProductSizes nếu có
                    if (!string.IsNullOrEmpty(request.ProductSizesJson))
                    {
                        try
                        {
                            var sizes = System.Text.Json.JsonSerializer.Deserialize<List<ProductSizeData>>(request.ProductSizesJson);
                            if (sizes != null && sizes.Count > 0)
                            {
                                foreach (var sizeData in sizes)
                                {
                                    if (!string.IsNullOrWhiteSpace(sizeData.Size) && sizeData.Price > 0)
                                    {
                                        var productSize = new ProductSize
                                        {
                                            ProductID = product.ProductID,
                                            Size = sizeData.Size.Trim().ToUpper(),
                                            Price = sizeData.Price
                                        };
                                        _db.ProductSizes.Add(productSize);
                                    }
                                }
                                await _db.SaveChangesAsync();
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            // Log lỗi parse JSON nhưng không fail toàn bộ request
                            System.Diagnostics.Debug.WriteLine("Error parsing ProductSizesJson: " + jsonEx.Message);
                        }
                    }
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Sửa Product
                    if (request.ProductId.HasValue)
                    {
                        var product = await _db.Products
                            .Include(p => p.ProductSizes)
                            .FirstOrDefaultAsync(p => p.ProductID == request.ProductId.Value);
                        if (product != null)
                        {
                            product.ProductName = request.ProductName;
                            product.CategoryID = request.CategoryID;
                            product.Description = request.Description;
                            product.Image_Url = request.Image_Url;
                            product.IsActive = request.IsActive;

                            // Xóa sizes cũ và thêm sizes mới
                            _db.ProductSizes.RemoveRange(product.ProductSizes);

                            // Thêm ProductSizes mới nếu có
                            if (!string.IsNullOrEmpty(request.ProductSizesJson))
                            {
                                try
                                {
                                    var sizes = System.Text.Json.JsonSerializer.Deserialize<List<ProductSizeData>>(request.ProductSizesJson);
                                    if (sizes != null && sizes.Count > 0)
                                    {
                                        foreach (var sizeData in sizes)
                                        {
                                            if (!string.IsNullOrWhiteSpace(sizeData.Size) && sizeData.Price > 0)
                                            {
                                                var productSize = new ProductSize
                                                {
                                                    ProductID = product.ProductID,
                                                    Size = sizeData.Size.Trim().ToUpper(),
                                                    Price = sizeData.Price
                                                };
                                                _db.ProductSizes.Add(productSize);
                                            }
                                        }
                                    }
                                }
                                catch (Exception jsonEx)
                                {
                                    System.Diagnostics.Debug.WriteLine("Error parsing ProductSizesJson: " + jsonEx.Message);
                                }
                            }
                        }
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa Product
                    if (request.ProductId.HasValue)
                    {
                        var product = await _db.Products
                            .Include(p => p.ProductSizes)
                            .FirstOrDefaultAsync(p => p.ProductID == request.ProductId.Value);
                        if (product != null)
                        {
                            // Xóa ProductSizes trước
                            _db.ProductSizes.RemoveRange(product.ProductSizes);
                            // Sau đó xóa Product
                            _db.Products.Remove(product);
                        }
                    }
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectProductRequest/{id} - Từ chối yêu cầu sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProductRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.ProductRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Rejected;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;
                request.RejectionReason = rejectionReason?.Trim();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã từ chối yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/ApproveCategoryRequest/{id} - Duyệt yêu cầu danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCategoryRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.CategoryRequests
                .Include(cr => cr.Category)
                .FirstOrDefaultAsync(cr => cr.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                // Thực hiện hành động dựa trên RequestType
                if (request.RequestType == RequestType.Add)
                {
                    // Tạo Category mới
                    var category = new ProductCategory
                    {
                        CategoryName = request.CategoryName
                    };
                    _db.ProductCategories.Add(category);
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Sửa Category
                    if (request.CategoryId.HasValue)
                    {
                        var category = await _db.ProductCategories
                            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId.Value);
                        if (category != null)
                        {
                            category.CategoryName = request.CategoryName;
                        }
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa Category (chỉ xóa nếu không có sản phẩm)
                    if (request.CategoryId.HasValue)
                    {
                        var category = await _db.ProductCategories
                            .Include(c => c.Products)
                            .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId.Value);
                        if (category != null)
                        {
                            // Kiểm tra xem có sản phẩm nào trong danh mục không
                            if (category.Products != null && category.Products.Any())
                            {
                                return Json(new { success = false, message = "Không thể xóa danh mục vì đang có sản phẩm trong danh mục này" });
                            }
                            _db.ProductCategories.Remove(category);
                        }
                    }
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectCategoryRequest/{id} - Từ chối yêu cầu danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCategoryRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.CategoryRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            request.Status = RequestStatus.Rejected;
            request.ReviewedBy = CurrentEmpId;
            request.ReviewedAt = DateTime.UtcNow;
            request.RejectionReason = rejectionReason?.Trim();

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Đã từ chối yêu cầu" });
        }

        // POST /Admin/ApproveBranchRequest/{id} - Duyệt yêu cầu chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBranchRequest(int id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.BranchRequests
                .Include(br => br.Branch)
                .Include(br => br.Region)
                .FirstOrDefaultAsync(br => br.Id == id);

            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Approved;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;

                // Thực hiện hành động dựa trên RequestType
                if (request.RequestType == RequestType.Add)
                {
                    // Kiểm tra tên branch không trùng trong cùng region
                    var existingBranch = await _db.Branches
                        .FirstOrDefaultAsync(b => b.Name == request.Name && b.RegionID == request.RegionID);

                    if (existingBranch != null)
                    {
                        return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{request.Name}\" trong region này" });
                    }

                    // Kiểm tra RegionID tồn tại
                    var region = await _db.Regions.FindAsync(request.RegionID);
                    if (region == null)
                    {
                        return Json(new { success = false, message = "Region không tồn tại" });
                    }

                    // Tạo Branch mới
                    var branch = new Branch
                    {
                        Name = request.Name,
                        Address = request.Address,
                        Phone = request.Phone,
                        RegionID = request.RegionID,
                        City = request.City,
                        Latitude = request.Latitude ?? 0,
                        Longitude = request.Longitude ?? 0
                    };
                    _db.Branches.Add(branch);
                }
                else if (request.RequestType == RequestType.Edit)
                {
                    // Sửa Branch
                    if (request.BranchId.HasValue)
                    {
                        var branch = await _db.Branches
                            .FirstOrDefaultAsync(b => b.BranchID == request.BranchId.Value);

                        if (branch == null)
                        {
                            return Json(new { success = false, message = "Chi nhánh không tồn tại" });
                        }

                        // Kiểm tra tên branch không trùng với branch khác trong cùng region
                        var existingBranch = await _db.Branches
                            .FirstOrDefaultAsync(b => b.Name == request.Name &&
                                                     b.RegionID == request.RegionID &&
                                                     b.BranchID != request.BranchId.Value);

                        if (existingBranch != null)
                        {
                            return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{request.Name}\" trong region này" });
                        }

                        // Cập nhật thông tin
                        branch.Name = request.Name;
                        branch.Address = request.Address;
                        branch.Phone = request.Phone;
                        branch.RegionID = request.RegionID;
                        branch.City = request.City;
                        if (request.Latitude.HasValue) branch.Latitude = request.Latitude.Value;
                        if (request.Longitude.HasValue) branch.Longitude = request.Longitude.Value;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Thiếu thông tin BranchId" });
                    }
                }
                else if (request.RequestType == RequestType.Delete)
                {
                    // Xóa Branch (soft delete - không xóa thực sự, chỉ đánh dấu)
                    if (request.BranchId.HasValue)
                    {
                        var branch = await _db.Branches
                            .Include(b => b.Orders)
                            .FirstOrDefaultAsync(b => b.BranchID == request.BranchId.Value);

                        if (branch == null)
                        {
                            return Json(new { success = false, message = "Chi nhánh không tồn tại" });
                        }

                        // Kiểm tra ràng buộc dữ liệu
                        var employeeCount = await _db.Employees
                            .CountAsync(e => e.BranchID == request.BranchId.Value);

                        var orderCount = branch.Orders?.Count ?? 0;

                        // Nếu có nhân viên hoặc đơn hàng, không cho phép xóa
                        // (Trong thực tế có thể cần thêm field IsActive vào Branch model)
                        if (employeeCount > 0 || orderCount > 0)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Không thể xóa chi nhánh vì đang có {employeeCount} nhân viên và {orderCount} đơn hàng liên quan. Vui lòng chuyển nhân viên và đơn hàng trước khi xóa."
                            });
                        }

                        // Xóa branch (hard delete - chỉ khi không có dữ liệu liên quan)
                        _db.Branches.Remove(branch);
                    }
                    else
                    {
                        return Json(new { success = false, message = "Thiếu thông tin BranchId" });
                    }
                }

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã duyệt yêu cầu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/RejectBranchRequest/{id} - Từ chối yêu cầu chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBranchRequest(int id, [FromForm] string? rejectionReason)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return Json(new { success = false, message = "Bạn cần đăng nhập" });

            var request = await _db.BranchRequests.FindAsync(id);
            if (request == null)
                return Json(new { success = false, message = "Yêu cầu không tồn tại" });

            if (request.Status != RequestStatus.Pending)
                return Json(new { success = false, message = "Yêu cầu này đã được xử lý" });

            try
            {
                request.Status = RequestStatus.Rejected;
                request.ReviewedBy = CurrentEmpId;
                request.ReviewedAt = DateTime.UtcNow;
                request.RejectionReason = rejectionReason?.Trim();

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã từ chối yêu cầu" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ VAI TRÒ (ROLE MANAGEMENT) - CHỈ DÀNH CHO AD ==========

        // Kiểm tra quyền truy cập: chỉ AD (Admin) mới được quản lý vai trò
        private bool CanManageRoles()
        {
            return CurrentRole == "AD";
        }

        // Kiểm tra xem user có được phép truy cập (AD, BM, RM) không
        private bool CanAccessAdminPanel()
        {
            var role = CurrentRole;
            return role == "AD" || role == "BM" || role == "RM";
        }

        // GET /Admin/Roles - Danh sách tất cả vai trò với thống kê
        public async Task<IActionResult> Roles()
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này. Chỉ Admin (AD) mới được phép quản lý vai trò.";
                if (CurrentRole == "AD")
                {
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    return RedirectToAction("Profile", "Employee");
                }
            }

            // Lấy tất cả vai trò, loại bỏ AD (Admin) vì đây là vai trò hệ thống
            var roles = await _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleID != "AD") // Loại bỏ vai trò AD
                .OrderBy(r => r.RoleID)
                .ToListAsync();

            // Tính số lượng nhân viên cho mỗi vai trò
            var rolesWithStats = new List<dynamic>();
            foreach (var role in roles)
            {
                var activeCount = await _db.Employees.CountAsync(e => e.RoleID == role.RoleID && e.IsActive);
                var totalCount = await _db.Employees.CountAsync(e => e.RoleID == role.RoleID);
                rolesWithStats.Add(new
                {
                    Role = role,
                    ActiveEmployeeCount = activeCount,
                    TotalEmployeeCount = totalCount,
                    InactiveEmployeeCount = totalCount - activeCount
                });
            }

            ViewBag.ActiveMenu = "Roles";
            ViewBag.RolesWithStats = rolesWithStats;
            ViewBag.CurrentRole = CurrentRole; // Truyền role hiện tại vào view để kiểm tra quyền
            return View(roles);
        }

        // GET /Admin/Roles/Employees/{roleId} - Xem danh sách nhân viên theo vai trò
        public IActionResult RoleEmployees(string roleId)
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này. Chỉ Admin (AD) mới được phép quản lý vai trò.";
                if (CurrentRole == "AD")
                {
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    return RedirectToAction("Profile", "Employee");
                }
            }

            if (string.IsNullOrEmpty(roleId))
            {
                return NotFound();
            }

            var role = _db.Roles
                .AsNoTracking()
                .FirstOrDefault(r => r.RoleID == roleId);

            if (role == null)
            {
                return NotFound();
            }

            var employees = _db.Employees
                .AsNoTracking()
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .Where(e => e.RoleID == roleId)
                .OrderBy(e => e.FullName)
                .ToList();

            ViewBag.ActiveMenu = "Roles";
            ViewBag.Role = role;
            ViewBag.Employees = employees;
            return View();
        }

        // POST /Admin/Roles/Create - Tạo vai trò mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleId, string roleName)
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này. Chỉ Admin (AD) mới được phép quản lý vai trò." });
            }

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(roleId) || roleId.Length > 2)
                {
                    return Json(new { success = false, message = "Mã vai trò phải có tối đa 2 ký tự." });
                }

                if (string.IsNullOrWhiteSpace(roleName) || roleName.Length > 50)
                {
                    return Json(new { success = false, message = "Tên vai trò phải có tối đa 50 ký tự." });
                }

                roleId = roleId.Trim().ToUpperInvariant();
                roleName = roleName.Trim();

                // Kiểm tra vai trò đã tồn tại chưa
                var existingRole = await _db.Roles.FindAsync(roleId);
                if (existingRole != null)
                {
                    return Json(new { success = false, message = "Mã vai trò đã tồn tại." });
                }

                // Tạo vai trò mới
                var newRole = new Role
                {
                    RoleID = roleId,
                    RoleName = roleName
                };

                _db.Roles.Add(newRole);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Tạo vai trò thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/Roles/Update - Cập nhật vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string roleId, string roleName)
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này. Chỉ Admin (AD) mới được phép quản lý vai trò." });
            }

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    return Json(new { success = false, message = "Mã vai trò không được để trống." });
                }

                if (string.IsNullOrWhiteSpace(roleName) || roleName.Length > 50)
                {
                    return Json(new { success = false, message = "Tên vai trò phải có tối đa 50 ký tự." });
                }

                roleId = roleId.Trim().ToUpperInvariant();
                roleName = roleName.Trim();

                // Tìm vai trò
                var role = await _db.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vai trò." });
                }

                // Chỉ Admin (AD) mới được sửa vai trò AD (đã được kiểm tra ở CanManageRoles)

                // Cập nhật tên vai trò
                role.RoleName = roleName;
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật vai trò thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST /Admin/Roles/Delete - Xóa vai trò
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này. Chỉ Admin (AD) mới được phép quản lý vai trò." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    return Json(new { success = false, message = "Mã vai trò không được để trống." });
                }

                roleId = roleId.Trim().ToUpperInvariant();

                // Không cho phép xóa vai trò AD (Admin) - bảo vệ vai trò hệ thống quan trọng
                if (roleId == "AD")
                {
                    return Json(new { success = false, message = "Không được phép xóa vai trò Admin (AD) - đây là vai trò hệ thống quan trọng." });
                }

                // Tìm vai trò
                var role = await _db.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy vai trò." });
                }

                // Xóa tất cả nhân viên có vai trò này trước (nếu có)
                var employeesWithRole = await _db.Employees.Where(e => e.RoleID == roleId).ToListAsync();
                var employeeCount = employeesWithRole.Count;
                if (employeesWithRole.Any())
                {
                    _db.Employees.RemoveRange(employeesWithRole);
                }

                // Xóa vai trò
                _db.Roles.Remove(role);
                await _db.SaveChangesAsync();

                var message = "Xóa vai trò thành công!";
                if (employeeCount > 0)
                {
                    message = $"Xóa vai trò thành công! Đã xóa {employeeCount} nhân viên có vai trò này.";
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/Roles/Statistics - API trả về thống kê vai trò (JSON)
        [HttpGet]
        public IActionResult RoleStatistics()
        {
            // Kiểm tra quyền: chỉ AD
            if (!CanManageRoles())
            {
                return Json(new { success = false, message = "Bạn không có quyền truy cập tính năng này. Chỉ Admin (AD) mới được phép quản lý vai trò." });
            }

            var statistics = _db.Roles
                .AsNoTracking()
                .Where(r => r.RoleID != "AD") // Loại bỏ vai trò AD
                .Select(r => new
                {
                    RoleID = r.RoleID,
                    RoleName = r.RoleName,
                    ActiveEmployees = _db.Employees.Count(e => e.RoleID == r.RoleID && e.IsActive),
                    TotalEmployees = _db.Employees.Count(e => e.RoleID == r.RoleID),
                    InactiveEmployees = _db.Employees.Count(e => e.RoleID == r.RoleID && !e.IsActive)
                })
                .OrderBy(x => x.RoleID)
                .ToList();

            return Json(new { success = true, data = statistics });
        }

        // Helper class để deserialize ProductSizes JSON
        private class ProductSizeData
        {
            public string Size { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        // ========== QUẢN LÝ REGION MANAGER (RM) ==========

        // Helper method để ghi AuditLog
        private async Task LogAuditAsync(string action, string? targetEmployeeId = null, string? targetEmployeeName = null, string? description = null, string entityType = "RM")
        {
            try
            {
                var admin = await _db.Employees.FindAsync(CurrentEmpId);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var auditLog = new AuditLog
                {
                    AdminId = CurrentEmpId ?? "",
                    AdminName = admin?.FullName,
                    Action = action,
                    TargetEmployeeId = targetEmployeeId,
                    TargetEmployeeName = targetEmployeeName,
                    Description = description,
                    EntityType = entityType,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng đến flow chính
                System.Diagnostics.Debug.WriteLine($"Error logging audit: {ex.Message}");
            }
        }

        // GET /Admin/RegionManagers - Danh sách tất cả Region Manager
        public IActionResult RegionManagers()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("Employees", new { tab = "regionManagers" });
        }

        // GET /Admin/RegionManagers/Create - Form tạo RM mới
        public async Task<IActionResult> CreateRegionManager()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "regionManagers" });
            }

            // Lấy danh sách Region để chọn
            var regions = await _db.Regions
                .OrderBy(r => r.RegionName)
                .ToListAsync();

            // Lấy danh sách Region đã có RM active
            var regionsWithRM = await _db.Employees
                .Where(e => e.RoleID == "RM" && e.IsActive && e.RegionID != null)
                .Select(e => e.RegionID.Value)
                .Distinct()
                .ToListAsync();

            ViewBag.Regions = regions;
            ViewBag.RegionsWithRM = regionsWithRM;
            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "regionManagers";

            return View();
        }

        // POST /Admin/RegionManagers/Create - Tạo RM mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRegionManager(
            string fullName,
            string email,
            string phoneNumber,
            DateTime? dateOfBirth,
            string? gender,
            string? city,
            int? regionId,
            string? nationality,
            string? ethnicity,
            string? emergencyPhone1,
            string? emergencyPhone2)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(fullName))
                    return Json(new { success = false, message = "Họ tên không được để trống." });

                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "Email không được để trống." });

                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống." });

                if (!regionId.HasValue)
                    return Json(new { success = false, message = "Vui lòng chọn vùng quản lý." });

                // Kiểm tra trùng email
                if (await _db.Employees.AnyAsync(e => e.Email == email))
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống." });

                // Kiểm tra trùng số điện thoại
                if (await _db.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại đã tồn tại trong hệ thống." });

                // Kiểm tra Region đã có RM active chưa
                var existingRM = await _db.Employees
                    .FirstOrDefaultAsync(e => e.RoleID == "RM" && e.RegionID == regionId.Value && e.IsActive);

                if (existingRM != null)
                    return Json(new { success = false, message = $"Vùng này đã có Region Manager đang hoạt động: {existingRM.FullName} ({existingRM.EmployeeID})." });

                // Tạo EmployeeID mới (RM + số thứ tự)
                var lastRM = await _db.Employees
                    .Where(e => e.RoleID == "RM" && e.EmployeeID != null && e.EmployeeID.StartsWith("RM"))
                    .OrderByDescending(e => e.EmployeeID)
                    .FirstOrDefaultAsync();

                string newRMId;
                if (lastRM != null && !string.IsNullOrEmpty(lastRM.EmployeeID) && lastRM.EmployeeID.Length >= 5)
                {
                    try
                    {
                        var numberPart = lastRM.EmployeeID.Substring(2);
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            newRMId = $"RM{(lastNumber + 1):D3}";
                        }
                        else
                        {
                            newRMId = "RM001";
                        }
                    }
                    catch
                    {
                        newRMId = "RM001";
                    }
                }
                else
                {
                    newRMId = "RM001";
                }

                // Lấy thông tin Region
                var region = await _db.Regions.FindAsync(regionId.Value);
                var regionName = region?.RegionName ?? regionId.Value.ToString();

                // Tạo RM mới
                var newRM = new Employee
                {
                    EmployeeID = newRMId,
                    FullName = fullName.Trim(),
                    Email = email.Trim(),
                    PhoneNumber = phoneNumber.Trim(),
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    City = city,
                    Nationality = nationality ?? "Việt Nam",
                    Ethnicity = ethnicity ?? "Kinh",
                    EmergencyPhone1 = emergencyPhone1,
                    EmergencyPhone2 = emergencyPhone2,
                    RoleID = "RM",
                    BranchID = null, // RM không gắn với chi nhánh cụ thể
                    RegionID = regionId.Value, // RM quản lý vùng
                    Password = _authService.HashPassword("1234567"), // Mật khẩu mặc định
                    IsHashed = true,
                    HireDate = DateTime.UtcNow,
                    IsActive = true,
                    AvatarUrl = "https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png"
                };

                _db.Employees.Add(newRM);
                await _db.SaveChangesAsync();

                // Ghi AuditLog
                await LogAuditAsync("CREATE_RM", newRMId, newRM.FullName,
                    $"Tạo Region Manager mới: {newRM.FullName} cho vùng {regionName}");

                TempData["SuccessMessage"] = $"Tạo Region Manager thành công! Mã nhân viên: {newRMId}";
                return Json(new { success = true, message = $"Tạo Region Manager thành công! Mã nhân viên: {newRMId}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/RegionManagers/Edit/{id} - Form sửa RM
        public async Task<IActionResult> EditRegionManager(string id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "regionManagers" });
            }

            var rm = await _db.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .Include(e => e.Region)
                .FirstOrDefaultAsync(e => e.EmployeeID == id && e.RoleID == "RM");

            if (rm == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Region Manager.";
                return RedirectToAction("Employees", new { tab = "regionManagers" });
            }

            // Lấy danh sách Region để chọn
            var regions = await _db.Regions
                .OrderBy(r => r.RegionName)
                .ToListAsync();

            // Lấy danh sách Region đã có RM active (trừ RM hiện tại)
            var regionsWithOtherRM = await _db.Employees
                .Where(e => e.RoleID == "RM" && e.IsActive && e.RegionID != null && e.EmployeeID != id)
                .Select(e => e.RegionID.Value)
                .Distinct()
                .ToListAsync();

            ViewBag.Regions = regions;
            ViewBag.RegionsWithRM = regionsWithOtherRM;
            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "regionManagers";

            return View(rm);
        }

        // POST /Admin/RegionManagers/Edit - Cập nhật RM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRegionManager(
            string employeeId,
            string fullName,
            string email,
            string phoneNumber,
            DateTime? dateOfBirth,
            string? gender,
            string? city,
            int? regionId,
            string? nationality,
            string? ethnicity,
            string? emergencyPhone1,
            string? emergencyPhone2)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                var rm = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId && e.RoleID == "RM");

                if (rm == null)
                    return Json(new { success = false, message = "Không tìm thấy Region Manager." });

                // Validation
                if (string.IsNullOrWhiteSpace(fullName))
                    return Json(new { success = false, message = "Họ tên không được để trống." });

                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "Email không được để trống." });

                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống." });

                if (!regionId.HasValue)
                    return Json(new { success = false, message = "Vui lòng chọn vùng quản lý." });

                // Kiểm tra trùng email (trừ chính nó)
                if (await _db.Employees.AnyAsync(e => e.Email == email && e.EmployeeID != employeeId))
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống." });

                // Kiểm tra trùng số điện thoại (trừ chính nó)
                if (await _db.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber && e.EmployeeID != employeeId))
                    return Json(new { success = false, message = "Số điện thoại đã tồn tại trong hệ thống." });

                // Kiểm tra Region đã có RM active khác chưa (nếu đổi region)
                if (regionId.HasValue && rm.RegionID != regionId.Value)
                {
                    var existingRM = await _db.Employees
                        .FirstOrDefaultAsync(e => e.RoleID == "RM" && e.RegionID == regionId.Value && e.IsActive && e.EmployeeID != employeeId);

                    if (existingRM != null)
                        return Json(new { success = false, message = $"Vùng này đã có Region Manager đang hoạt động: {existingRM.FullName} ({existingRM.EmployeeID})." });
                }

                // Lưu thông tin cũ để ghi audit log
                var oldRegionId = rm.RegionID;

                // Cập nhật thông tin Employee
                rm.FullName = fullName.Trim();
                rm.Email = email.Trim();
                rm.PhoneNumber = phoneNumber.Trim();
                rm.DateOfBirth = dateOfBirth;
                rm.Gender = gender;
                rm.City = city;
                rm.Nationality = nationality ?? "Việt Nam";
                rm.Ethnicity = ethnicity ?? "Kinh";
                rm.EmergencyPhone1 = emergencyPhone1;
                rm.EmergencyPhone2 = emergencyPhone2;
                if (regionId.HasValue)
                {
                    rm.RegionID = regionId.Value;
                }

                await _db.SaveChangesAsync();

                // Ghi AuditLog
                var region = await _db.Regions.FindAsync(rm.RegionID);
                var regionName = region?.RegionName ?? rm.RegionID?.ToString() ?? "";
                var description = $"Cập nhật thông tin Region Manager: {rm.FullName}";
                if (oldRegionId != rm.RegionID)
                {
                    var oldRegion = await _db.Regions.FindAsync(oldRegionId);
                    var oldRegionName = oldRegion?.RegionName ?? oldRegionId?.ToString() ?? "";
                    description += $". Đổi vùng từ {oldRegionName} sang {regionName}";
                }
                await LogAuditAsync("UPDATE_RM", rm.EmployeeID, rm.FullName, description);

                TempData["SuccessMessage"] = "Cập nhật thông tin Region Manager thành công!";
                return Json(new { success = true, message = "Cập nhật thông tin Region Manager thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/RegionManagers/View/{id} - Xem chi tiết RM
        public async Task<IActionResult> ViewRegionManager(string id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "regionManagers" });
            }

            var rm = await _db.Employees
                .AsNoTracking()
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .Include(e => e.Region)
                .FirstOrDefaultAsync(e => e.EmployeeID == id && e.RoleID == "RM");

            if (rm == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Region Manager.";
                return RedirectToAction("Employees", new { tab = "regionManagers" });
            }

            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "regionManagers";

            return View(rm);
        }

        // POST /Admin/RegionManagers/ToggleStatus - Kích hoạt/Vô hiệu hóa RM (không xóa vĩnh viễn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRegionManagerStatus(string employeeId)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                var rm = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId && e.RoleID == "RM");

                if (rm == null)
                    return Json(new { success = false, message = "Không tìm thấy Region Manager." });

                var oldStatus = rm.IsActive;
                rm.IsActive = !rm.IsActive;

                await _db.SaveChangesAsync();

                // Ghi AuditLog
                var action = rm.IsActive ? "ACTIVATE_RM" : "DEACTIVATE_RM";
                var description = rm.IsActive
                    ? $"Kích hoạt Region Manager: {rm.FullName}"
                    : $"Vô hiệu hóa Region Manager: {rm.FullName}";
                await LogAuditAsync(action, rm.EmployeeID, rm.FullName, description);

                var message = rm.IsActive
                    ? $"Đã kích hoạt Region Manager: {rm.FullName}"
                    : $"Đã vô hiệu hóa Region Manager: {rm.FullName}";

                return Json(new { success = true, message = message, isActive = rm.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ========== QUẢN LÝ MARKETING MANAGER (MK) ==========

        // GET /Admin/MarketingManagers - Danh sách tất cả Marketing Manager
        public IActionResult MarketingManagers()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("Employees", new { tab = "marketingManagers" });
        }

        // GET /Admin/MarketingManagers/Create - Form tạo MK mới
        public async Task<IActionResult> CreateMarketingManager()
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "marketingManagers" });
            }

            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "marketingManagers";

            return View();
        }

        // POST /Admin/MarketingManagers/Create - Tạo MK mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMarketingManager(
            string fullName,
            string email,
            string phoneNumber,
            DateTime? dateOfBirth,
            string? gender,
            string? city,
            string? nationality,
            string? ethnicity,
            string? emergencyPhone1,
            string? emergencyPhone2)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(fullName))
                    return Json(new { success = false, message = "Họ tên không được để trống." });

                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "Email không được để trống." });

                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống." });

                // Kiểm tra trùng email
                if (await _db.Employees.AnyAsync(e => e.Email == email))
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống." });

                // Kiểm tra trùng số điện thoại
                if (await _db.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại đã tồn tại trong hệ thống." });

                // Tạo EmployeeID mới (MK + số thứ tự)
                var lastMK = await _db.Employees
                    .Where(e => e.RoleID == "MK" && e.EmployeeID != null && e.EmployeeID.StartsWith("MK"))
                    .OrderByDescending(e => e.EmployeeID)
                    .FirstOrDefaultAsync();

                string newMKId;
                if (lastMK != null && !string.IsNullOrEmpty(lastMK.EmployeeID) && lastMK.EmployeeID.Length >= 5)
                {
                    try
                    {
                        var numberPart = lastMK.EmployeeID.Substring(2);
                        if (int.TryParse(numberPart, out int lastNumber))
                        {
                            newMKId = $"MK{(lastNumber + 1):D3}";
                        }
                        else
                        {
                            newMKId = "MK001";
                        }
                    }
                    catch
                    {
                        newMKId = "MK001";
                    }
                }
                else
                {
                    newMKId = "MK001";
                }

                // Tạo MK mới
                var newMK = new Employee
                {
                    EmployeeID = newMKId,
                    FullName = fullName.Trim(),
                    Email = email.Trim(),
                    PhoneNumber = phoneNumber.Trim(),
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    City = city,
                    Nationality = nationality ?? "Việt Nam",
                    Ethnicity = ethnicity ?? "Kinh",
                    EmergencyPhone1 = emergencyPhone1,
                    EmergencyPhone2 = emergencyPhone2,
                    RoleID = "MK",
                    BranchID = null, // MK không gắn với chi nhánh cụ thể
                    RegionID = null, // MK không gắn với vùng cụ thể
                    Password = _authService.HashPassword("1234567"), // Mật khẩu mặc định
                    IsHashed = true,
                    HireDate = DateTime.UtcNow,
                    IsActive = true,
                    AvatarUrl = "https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png"
                };

                _db.Employees.Add(newMK);
                await _db.SaveChangesAsync();

                // Ghi AuditLog
                await LogAuditAsync("CREATE_MK", newMKId, newMK.FullName,
                    $"Tạo Marketing Manager mới: {newMK.FullName}", "MK");

                TempData["SuccessMessage"] = $"Tạo Marketing Manager thành công! Mã nhân viên: {newMKId}";
                return Json(new { success = true, message = $"Tạo Marketing Manager thành công! Mã nhân viên: {newMKId}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/MarketingManagers/Edit/{id} - Form sửa MK
        public async Task<IActionResult> EditMarketingManager(string id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "marketingManagers" });
            }

            var mm = await _db.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeID == id && e.RoleID == "MK");

            if (mm == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Marketing Manager.";
                return RedirectToAction("Employees", new { tab = "marketingManagers" });
            }

            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "marketingManagers";

            return View(mm);
        }

        // POST /Admin/MarketingManagers/Edit - Cập nhật MK
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMarketingManager(
            string employeeId,
            string fullName,
            string email,
            string phoneNumber,
            DateTime? dateOfBirth,
            string? gender,
            string? city,
            string? nationality,
            string? ethnicity,
            string? emergencyPhone1,
            string? emergencyPhone2)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                var mm = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId && e.RoleID == "MK");

                if (mm == null)
                    return Json(new { success = false, message = "Không tìm thấy Marketing Manager." });

                // Validation
                if (string.IsNullOrWhiteSpace(fullName))
                    return Json(new { success = false, message = "Họ tên không được để trống." });

                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, message = "Email không được để trống." });

                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return Json(new { success = false, message = "Số điện thoại không được để trống." });

                // Kiểm tra trùng email (trừ chính nó)
                if (await _db.Employees.AnyAsync(e => e.Email == email && e.EmployeeID != employeeId))
                    return Json(new { success = false, message = "Email đã tồn tại trong hệ thống." });

                // Kiểm tra trùng số điện thoại (trừ chính nó)
                if (await _db.Employees.AnyAsync(e => e.PhoneNumber == phoneNumber && e.EmployeeID != employeeId))
                    return Json(new { success = false, message = "Số điện thoại đã tồn tại trong hệ thống." });

                // Cập nhật thông tin
                mm.FullName = fullName.Trim();
                mm.Email = email.Trim();
                mm.PhoneNumber = phoneNumber.Trim();
                mm.DateOfBirth = dateOfBirth;
                mm.Gender = gender;
                mm.City = city;
                mm.Nationality = nationality ?? "Việt Nam";
                mm.Ethnicity = ethnicity ?? "Kinh";
                mm.EmergencyPhone1 = emergencyPhone1;
                mm.EmergencyPhone2 = emergencyPhone2;

                await _db.SaveChangesAsync();

                // Ghi AuditLog
                await LogAuditAsync("UPDATE_MK", employeeId, mm.FullName,
                    $"Cập nhật thông tin Marketing Manager: {mm.FullName}", "MK");

                return Json(new { success = true, message = "Cập nhật Marketing Manager thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET /Admin/MarketingManagers/View/{id} - Xem chi tiết MK
        public async Task<IActionResult> ViewMarketingManager(string id)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập tính năng này.";
                return RedirectToAction("Employees", new { tab = "marketingManagers" });
            }

            var mm = await _db.Employees
                .Include(e => e.Branch)
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeID == id && e.RoleID == "MK");

            if (mm == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Marketing Manager.";
                return RedirectToAction("Employees", new { tab = "marketingManagers" });
            }

            ViewBag.ActiveMenu = "Employees";
            ViewBag.CurrentRole = CurrentRole;
            ViewBag.ActiveTab = "marketingManagers";

            return View(mm);
        }

        // POST /Admin/MarketingManagers/ToggleStatus - Bật/tắt trạng thái MK
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMarketingManagerStatus(string employeeId)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || CurrentRole != "AD")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            try
            {
                var mm = await _db.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId && e.RoleID == "MK");

                if (mm == null)
                    return Json(new { success = false, message = "Không tìm thấy Marketing Manager." });

                var oldStatus = mm.IsActive;
                mm.IsActive = !mm.IsActive;

                await _db.SaveChangesAsync();

                // Ghi AuditLog
                var action = mm.IsActive ? "ACTIVATE_MK" : "DEACTIVATE_MK";
                var description = mm.IsActive
                    ? $"Đã kích hoạt Marketing Manager: {mm.FullName}"
                    : $"Đã vô hiệu hóa Marketing Manager: {mm.FullName}";

                await LogAuditAsync(action, employeeId, mm.FullName, description, "MK");

                var message = mm.IsActive
                    ? $"Đã kích hoạt Marketing Manager: {mm.FullName}"
                    : $"Đã vô hiệu hóa Marketing Manager: {mm.FullName}";

                return Json(new { success = true, message = message, isActive = mm.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}

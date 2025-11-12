
// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;
using start.Models;      
using start.Data;           // Employee, EditEmployeeProfile
using start.Services;               // IEmployeeProfileService
using start.Models.ViewModels;
namespace start.Controllers
{
    
    [Route("Employee")] // /Employee/...
  [Authorize(AuthenticationSchemes = "EmployeeScheme")]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeProfileService _svc;
        private readonly IScheduleService _s;
        private readonly IPayrollService _p;
        private readonly ApplicationDbContext _db;
        private readonly IDayOffService _dayoff;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMarketingKPIService _kpiService;

        public EmployeeController(ApplicationDbContext db, IEmployeeProfileService svc, IScheduleService s, IPayrollService p, IDayOffService dayoff, ICloudinaryService cloudinaryService, IMarketingKPIService kpiService)
        {
            _db = db;
            _svc = svc;
            _s = s;
            _p = p;
            _dayoff = dayoff;
            _cloudinaryService = cloudinaryService;
            _kpiService = kpiService;
        }

        // Lấy EmployeeID và Role từ Claims
        // Roles: AD (Admin), BM (Branch Manager), EM (Employee), RM (Region Manager), SL (Shift Leader)
        private string? CurrentEmpId => User.FindFirst("EmployeeID")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? CurrentRole => User.FindFirst("Role")?.Value?.Trim().ToUpperInvariant();

        // CHO PHÉP: EM (Employee) | SL (Shift Leader)
        private bool CanAccessDayOff() =>
            CurrentRole is "EM" or "SL";
        private bool IsMarketing() => CurrentRole == "MK";

        // GET /Employee/MarketingDashboard - Dashboard cho Marketing để xem request status
        [HttpGet("MarketingDashboard")]
        public async Task<IActionResult> MarketingDashboard(string? status, string? requestType, int? month, int? year, int page = 1, int pageSize = 15)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || !IsMarketing())
                return RedirectToAction("Profile");

            var now = DateTime.Now;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;
            var startDate = new DateTime(selectedYear, selectedMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Query NewsRequests của user hiện tại
            var newsQuery = _db.NewsRequests
                .AsNoTracking()
                .Include(nr => nr.ReviewedByEmployee)
                .Where(nr => nr.RequestedBy == CurrentEmpId)
                .AsQueryable();

            // Query DiscountRequests của user hiện tại
            var discountQuery = _db.DiscountRequests
                .AsNoTracking()
                .Include(dr => dr.ReviewedByEmployee)
                .Where(dr => dr.RequestedBy == CurrentEmpId)
                .AsQueryable();

            // Lọc theo tháng/năm nếu có
            if (month.HasValue && year.HasValue)
            {
                newsQuery = newsQuery.Where(nr => nr.RequestedAt >= startDate && nr.RequestedAt <= endDate);
                discountQuery = discountQuery.Where(dr => dr.RequestedAt >= startDate && dr.RequestedAt <= endDate);
            }

            // Lọc theo loại request
            if (requestType == "news")
            {
                discountQuery = discountQuery.Where(dr => false); // Không hiển thị Discount
            }
            else if (requestType == "discount")
            {
                newsQuery = newsQuery.Where(nr => false); // Không hiển thị News
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    newsQuery = newsQuery.Where(nr => nr.Status == statusEnum);
                    discountQuery = discountQuery.Where(dr => dr.Status == statusEnum);
                }
            }

            // Lấy danh sách NewsRequests
            var newsRequests = newsQuery
                .OrderByDescending(nr => nr.RequestedAt)
                .ToList();

            // Lấy danh sách DiscountRequests
            var discountRequests = discountQuery
                .OrderByDescending(dr => dr.RequestedAt)
                .ToList();

            // Kết hợp và phân trang
            var allRequestsList = new List<Dictionary<string, object>>();
            
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
            
            // Sắp xếp: Status trước, sau đó RequestedAt
            var allRequests = allRequestsList
                .OrderBy(x => (RequestStatus)x["Status"])      // Pending=0, Approved=1, Rejected=2
                .ThenByDescending(x => (DateTime)x["RequestedAt"])  // Mới nhất trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Tính tổng số theo trạng thái (tất cả thời gian)
            var totalPending = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Pending)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Pending);
            
            var totalApproved = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Approved)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Approved);
            
            var totalRejected = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Rejected)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Rejected);

            // Tính tổng số theo trạng thái trong tháng được chọn
            var monthPending = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Pending && nr.RequestedAt >= startDate && nr.RequestedAt <= endDate)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Pending && dr.RequestedAt >= startDate && dr.RequestedAt <= endDate);
            
            var monthApproved = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Approved && nr.RequestedAt >= startDate && nr.RequestedAt <= endDate)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Approved && dr.RequestedAt >= startDate && dr.RequestedAt <= endDate);
            
            var monthRejected = _db.NewsRequests.Count(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Rejected && nr.RequestedAt >= startDate && nr.RequestedAt <= endDate)
                + _db.DiscountRequests.Count(dr => dr.RequestedBy == CurrentEmpId && dr.Status == RequestStatus.Rejected && dr.RequestedAt >= startDate && dr.RequestedAt <= endDate);

            // Lấy KPI tháng hiện tại
            var currentKPI = await _kpiService.GetKPIAsync(CurrentEmpId, selectedYear, selectedMonth);
            if (currentKPI == null)
            {
                currentKPI = await _kpiService.CalculateAndSaveKPIAsync(CurrentEmpId, selectedYear, selectedMonth);
            }

            // Lấy tin tức được duyệt gần nhất (chiến dịch nổi bật)
            var latestApprovedNews = _db.NewsRequests
                .AsNoTracking()
                .Where(nr => nr.RequestedBy == CurrentEmpId && nr.Status == RequestStatus.Approved)
                .OrderByDescending(nr => nr.ReviewedAt ?? nr.RequestedAt)
                .FirstOrDefault();

            // Lấy dữ liệu cho biểu đồ KPI 6 tháng gần nhất
            var kpiChartData = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var chartMonth = now.AddMonths(-i);
                var kpi = await _kpiService.GetKPIAsync(CurrentEmpId, chartMonth.Year, chartMonth.Month);
                if (kpi == null)
                {
                    kpi = await _kpiService.CalculateAndSaveKPIAsync(CurrentEmpId, chartMonth.Year, chartMonth.Month);
                }
                kpiChartData.Add(new
                {
                    Month = chartMonth.ToString("MM/yyyy"),
                    KPIScore = kpi?.KPIScore ?? 0,
                    ApprovedCount = kpi?.TotalApproved ?? 0
                });
            }

            // Lấy timeline (10 requests gần nhất)
            var timelineRequests = allRequestsList
                .OrderByDescending(x => (DateTime)x["RequestedAt"])
                .Take(10)
                .ToList();

            var totalCount = allRequestsList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            ViewBag.CurrentStatus = status ?? "all";
            ViewBag.CurrentRequestType = requestType ?? "all";
            ViewBag.CurrentMonth = selectedMonth;
            ViewBag.CurrentYear = selectedYear;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPending = totalPending;
            ViewBag.TotalApproved = totalApproved;
            ViewBag.TotalRejected = totalRejected;
            ViewBag.MonthPending = monthPending;
            ViewBag.MonthApproved = monthApproved;
            ViewBag.MonthRejected = monthRejected;
            ViewBag.CurrentKPI = currentKPI;
            ViewBag.LatestApprovedNews = latestApprovedNews;
            ViewBag.KPIChartData = kpiChartData;
            ViewBag.TimelineRequests = timelineRequests;
            ViewBag.ActiveMenu = "MarketingDashboard";

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);

            ViewBag.Employee = emp;
            return View(allRequests);
        }

        // GET /Employee  (Hồ sơ)
        [HttpGet]
        public IActionResult Profile()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);

            if (emp == null) return NotFound();
            return View(emp);            // Views/Employee/Profile.cshtml
        }
        // GET /Employee/Edit  (Form chỉnh sửa)
        [HttpGet("Edit")]
        public IActionResult EditProfile()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            var emp = _svc.GetById(CurrentEmpId!);
            if (emp == null) return NotFound();

            // map Entity -> EditEmployeeProfile (model form)
            var vm = new EditEmployeeProfile
            {
                DateOfBirth = emp.DateOfBirth,
                Nationality = emp.Nationality,
                Gender = emp.Gender,
                Ethnicity = emp.Ethnicity,
                PhoneNumber = emp.PhoneNumber,
                Email = emp.Email,
                EmergencyPhone1 = emp.EmergencyPhone1,
                EmergencyPhone2 = emp.EmergencyPhone2
            };

            ViewBag.ActiveMenu = "EditProfile";
            ViewBag.Employee = emp; // Thêm vào ViewBag để view dễ truy cập
            ViewBag.CurrentRole = CurrentRole; // Truyền role vào ViewBag
            ViewData["Employee"] = emp;
            return View("EditProfile", vm);      // Views/Employee/EditProfile.cshtml  @model EditEmployeeProfile
        }

        // POST /Employee/Edit  (Lưu chỉnh sửa)
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditEmployeeProfile model)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var emp = _svc.GetById(CurrentEmpId!);
                ViewBag.ActiveMenu = "EditProfile";
                ViewBag.CurrentRole = CurrentRole; // Truyền role vào ViewBag
                ViewBag.Employee = emp;
                ViewData["Employee"] = emp;
                return View("EditProfile", model);
            }

            var ok = _svc.EditProfile(CurrentEmpId!, model, out var error);
            if (!ok)
            {
                var emp = _svc.GetById(CurrentEmpId!);
                ModelState.AddModelError(string.Empty, error);
                ViewBag.ActiveMenu = "EditProfile";
                ViewBag.CurrentRole = CurrentRole; // Truyền role vào ViewBag
                ViewBag.Employee = emp;
                ViewData["Employee"] = emp;
                return View("EditProfile", model);
            }

            TempData["ok"] = "Đã lưu thay đổi.";
            return RedirectToAction(nameof(EditProfile));
        }

        // POST /Employee/UploadAvatar  (Upload ảnh đại diện)
        [HttpPost("UploadAvatar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");

            if (avatar == null || avatar.Length == 0)
            {
                TempData["err"] = "File ảnh không hợp lệ.";
                return RedirectToAction(nameof(EditProfile));
            }

            var ok = await _svc.UploadAvatar(CurrentEmpId!, avatar);
            if (!ok) TempData["err"] = "Upload ảnh thất bại.";
            else TempData["ok"] = "Đã cập nhật ảnh đại diện.";

            return RedirectToAction(nameof(EditProfile));
        }
        [HttpGet("Chat")]
        public IActionResult Chat(string? id)
        {
            id ??= CurrentEmpId;
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login", "Account");

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)         // <-- quan trọng
                         .SingleOrDefault(e => e.EmployeeID == id);

            if (emp == null) return NotFound();

            ViewBag.ActiveMenu = "Profile";
            ViewBag.ActiveTab = "chat";
            return View(emp);
        }
        [HttpGet("Contract/{id}")]
        public IActionResult Contract(string id)
        {
            ViewBag.ActiveTab = "contract";

            var contract = _db.Contracts
                              .Include(c => c.Employee)              // load Employee
                              .ThenInclude(e => e.Branch)            // load Branch của Employee
                              .AsNoTracking()
                              .OrderByDescending(c => c.StartDate)
                              .FirstOrDefault(c => c.EmployeeId == id);

            if (contract == null) return NotFound();

            ViewBag.ActiveMenu = "Profile";
            return View("Contract", contract);  // model = Contract
        }
        [HttpGet("Schedule/{id?}")]
        public IActionResult Schedule(string? id, int? month, int? year)
        {
            id ??= CurrentEmpId;
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;
            int m = month ?? today.Month;
            int y = year ?? today.Year;

            var dto = _s.GetMonthSchedule(id, m, y);

            // Có thể dùng dto trực tiếp trong view thay vì tạo VM riêng
            ViewBag.ActiveMenu = "Profile";
            ViewBag.ActiveTab = "schedule";
            return View("Schedule", dto); // Views/Employee/Schedule.cshtml @model MonthScheduleDto
        }
 [HttpGet("salary")]
    public async Task<IActionResult> Salary(string id, int? month, int? year)
    {
        ViewBag.ActiveMenu = "Profile";
        ViewBag.ActiveTab  = "salary";

        var now = DateTime.Today;
        int m = (month is >= 1 and <= 12) ? month.Value : now.Month;
        int y = (year  is >= 2000)        ? year.Value  : now.Year;

        // >>> LẤY EMPLOYEE KÈM BRANCH (và Role nếu cần)
        var emp = await _db.Employees
            .Include(e => e.Branch)
            .Include(e => e.Role)          // (tuỳ)
            .FirstOrDefaultAsync(e => e.EmployeeID == id);

        // Bảng lương
        var vm = await _p.GetMonthlySalaryAsync(id, y, m);

        // Nếu là Marketing employee, lấy thông tin KPI
        if (emp?.RoleID == "MK" && vm != null)
        {
            var kpi = await _kpiService.GetKPIAsync(id, y, m);
            if (kpi == null)
            {
                // Nếu chưa có KPI, tính và lưu
                kpi = await _kpiService.CalculateAndSaveKPIAsync(id, y, m);
            }
            ViewBag.KPI = kpi;
        }

        // Truyền cho view/partials
        ViewBag.Employee = emp;
        ViewData["EmployeeID"] = id;
        ViewData["Month"] = m;
        ViewData["Year"]  = y;

        return View(vm); // Views/Employee/Salary.cshtml (model: MonthlySalaryVm?)
    }
[HttpGet("DayOff/{id?}")]
public async Task<IActionResult> DayOff(string? id)
{
    // THÊM kiểm tra đăng nhập trước
    id ??= CurrentEmpId;
    if (string.IsNullOrEmpty(id))
        return RedirectToAction("Login", "Account");

    // THÊM CHẶN QUYỀN
    if (!CanAccessDayOff())
        return Forbid(); // hoặc RedirectToAction("Profile")

    // GỘP thành 1 lần query emp (xóa dòng query trùng ngay bên dưới của bạn)
    var emp = await _db.Employees
        .Include(e => e.Branch)
        .Include(e => e.Role) // nếu cần
        .FirstOrDefaultAsync(e => e.EmployeeID == id);

    if (emp == null) return NotFound();

    ViewBag.ActiveMenu = "DayOff";
    ViewBag.Employee = emp;
    ViewBag.Requests = await _dayoff.GetMyAsync(id);

    var vm = new DayOffOneDayVm {
        EmployeeID = id,
        BranchID   = emp.BranchID,
        OffDate    = DateTime.Today.AddDays(3)
    };
    return View("DayOff", vm);
}

  [HttpPost("DayOff")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DayOffSubmit(DayOffOneDayVm vm)
{
    // THÊM CHẶN QUYỀN NGAY ĐẦU
    if (!CanAccessDayOff())
        return Forbid();

    if (vm.OffDate.Date < DateTime.Today.AddDays(3))
        ModelState.AddModelError(nameof(vm.OffDate), "Ngày nghỉ phải sau hôm nay ít nhất 3 ngày.");

    if (!ModelState.IsValid)
    {
        var emp = await _db.Employees.FindAsync(vm.EmployeeID);
        ViewBag.Employee = emp;
        ViewBag.Requests = await _dayoff.GetMyAsync(vm.EmployeeID);
        return View("DayOff", vm);
    }

    try
    {
        await _dayoff.CreateOneDayAsync(vm);
        TempData["ok"] = "Đã gửi yêu cầu nghỉ 1 ngày tới quản lý.";
    }
    catch (Exception ex)
    {
        TempData["err"] = ex.Message;
    }

    return RedirectToAction("DayOff", new { id = vm.EmployeeID });
}



        // ========== MARKETING: Tạo yêu cầu News ==========
        [HttpGet("CreateNewsRequest")]
        public IActionResult CreateNewsRequest()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");
            if (!IsMarketing()) return Forbid();
            ViewBag.ActiveMenu = "CreateNewsRequest";
            var emp = _db.Employees
                         .Include(e => e.Branch)
                         .AsNoTracking()
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);
            
            // Load danh sách mã giảm giá đang active để chọn
            var activeDiscounts = _db.Discounts
                .Where(d => d.IsActive && (d.EndAt == null || d.EndAt > DateTime.UtcNow))
                .OrderByDescending(d => d.StartAt ?? DateTime.MinValue)
                .Select(d => new { d.Id, d.Code, d.Type, d.Percent, d.Amount })
                .ToList();
            ViewBag.ActiveDiscounts = activeDiscounts;
            
            return View(emp); // Views/Employee/CreateNewsRequest.cshtml
        }

        [HttpPost("CreateNewsRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewsRequest([FromForm] string title, [FromForm] string content, IFormFile? imageFile, [FromForm] int? discountId)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");
            if (!IsMarketing()) return Forbid();

            if (string.IsNullOrWhiteSpace(title))
                ModelState.AddModelError(nameof(title), "Tiêu đề không được để trống.");
            if (string.IsNullOrWhiteSpace(content))
                ModelState.AddModelError(nameof(content), "Nội dung không được để trống.");

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh: JPG, PNG, GIF, WEBP.");
                }
                else if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("imageFile", "Kích thước file không được vượt quá 5MB.");
                }
                else
                {
                    // Upload lên Cloudinary
                    imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "uploads/news");
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        ModelState.AddModelError("imageFile", "Upload ảnh thất bại. Vui lòng thử lại.");
                    }
                }
            }

            // Validate discountId nếu có
            if (discountId.HasValue)
            {
                var discountExists = await _db.Discounts.AnyAsync(d => d.Id == discountId.Value && d.IsActive);
                if (!discountExists)
                {
                    ModelState.AddModelError("discountId", "Mã giảm giá không hợp lệ hoặc đã hết hạn.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveMenu = "CreateNewsRequest";
                ViewBag.TitleValue = title;
                ViewBag.ContentValue = content;
                ViewBag.DiscountIdValue = discountId;
                var activeDiscounts = _db.Discounts
                    .Where(d => d.IsActive && (d.EndAt == null || d.EndAt > DateTime.UtcNow))
                    .OrderByDescending(d => d.StartAt ?? DateTime.MinValue)
                    .Select(d => new { d.Id, d.Code, d.Type, d.Percent, d.Amount })
                    .ToList();
                ViewBag.ActiveDiscounts = activeDiscounts;
                var emp = _db.Employees.AsNoTracking().Include(e => e.Branch).SingleOrDefault(e => e.EmployeeID == CurrentEmpId);
                return View(emp);
            }

            var req = new NewsRequest
            {
                RequestType = RequestType.Add,
                NewsId = null,
                RequestedBy = CurrentEmpId!,
                RequestedAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                Title = title.Trim(),
                Content = content,
                ImageUrl = imageUrl,
                DiscountId = discountId,
                CreatedAt = DateTime.UtcNow
            };
            _db.NewsRequests.Add(req);
            _db.SaveChanges();
            TempData["ok"] = "Đã gửi yêu cầu tạo tin tức tới Admin duyệt.";
            return RedirectToAction("MarketingDashboard");
        }

        // ========== MARKETING: Tạo yêu cầu Discount ==========
        [HttpGet("CreateDiscountRequest")]
        public IActionResult CreateDiscountRequest()
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");
            if (!IsMarketing()) return Forbid();
            ViewBag.ActiveMenu = "CreateDiscountRequest";
            var emp = _db.Employees
                         .Include(e => e.Branch)
                         .AsNoTracking()
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);
            return View(emp); // Views/Employee/CreateDiscountRequest.cshtml
        }

        [HttpPost("CreateDiscountRequest")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDiscountRequest([FromForm] string code,
                                                   [FromForm] decimal? percent,
                                                   [FromForm] decimal? amount,
                                                   [FromForm] DiscountType type,
                                                   [FromForm] DateTime? startAt,
                                                   [FromForm] DateTime? endAt,
                                                   [FromForm] int? usageLimit)
        {
            if (string.IsNullOrEmpty(CurrentEmpId))
                return RedirectToAction("Login", "Account");
            if (!IsMarketing()) return Forbid();

            if (string.IsNullOrWhiteSpace(code))
                ModelState.AddModelError(nameof(code), "Mã giảm giá không được để trống.");
            if (type == DiscountType.Percentage && (percent is null or <= 0 or > 100))
                ModelState.AddModelError(nameof(percent), "Phần trăm giảm phải trong khoảng 0-100.");
            if (type == DiscountType.FixedAmount && (amount is null or <= 0))
                ModelState.AddModelError(nameof(amount), "Số tiền giảm phải lớn hơn 0.");
            if (startAt.HasValue && endAt.HasValue && endAt < startAt)
                ModelState.AddModelError(nameof(endAt), "Ngày kết thúc phải sau ngày bắt đầu.");

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveMenu = "CreateDiscountRequest";
                ViewBag.CodeValue = code;
                ViewBag.PercentValue = percent;
                ViewBag.AmountValue = amount;
                ViewBag.TypeValue = (int)type;
                ViewBag.StartAtValue = startAt?.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.EndAtValue = endAt?.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.UsageLimitValue = usageLimit;
                var emp = _db.Employees.AsNoTracking().Include(e => e.Branch).SingleOrDefault(e => e.EmployeeID == CurrentEmpId);
                return View(emp);
            }

            var req = new DiscountRequest
            {
                RequestType = RequestType.Add,
                DiscountId = null,
                RequestedBy = CurrentEmpId!,
                RequestedAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                Code = code.Trim().ToUpperInvariant(),
                Percent = type == DiscountType.Percentage ? (percent ?? 0) : 0,
                Amount = type == DiscountType.FixedAmount ? (amount ?? 0) : null,
                StartAt = startAt,
                EndAt = endAt,
                IsActive = true,
                UsageLimit = usageLimit,
                Type = type
            };
            _db.DiscountRequests.Add(req);
            _db.SaveChanges();
            TempData["ok"] = "Đã gửi yêu cầu tạo mã giảm giá tới Admin duyệt.";
            return RedirectToAction("MarketingDashboard");
        }

        // GET /Employee/MarketingKPI - Xem KPI của Marketing
        [HttpGet("MarketingKPI")]
        public async Task<IActionResult> MarketingKPI(int? year, int? month)
        {
            if (string.IsNullOrEmpty(CurrentEmpId) || !IsMarketing())
                return RedirectToAction("Profile");

            var now = DateTime.Now;
            int y = year ?? now.Year;
            int m = month ?? now.Month;

            // Tính hoặc lấy KPI
            var kpi = await _kpiService.CalculateAndSaveKPIAsync(CurrentEmpId, y, m);

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .SingleOrDefault(e => e.EmployeeID == CurrentEmpId);

            ViewBag.Employee = emp;
            ViewBag.ActiveMenu = "MarketingKPI";
            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.PreviousMonth = m == 1 ? 12 : m - 1;
            ViewBag.PreviousYear = m == 1 ? y - 1 : y;
            ViewBag.NextMonth = m == 12 ? 1 : m + 1;
            ViewBag.NextYear = m == 12 ? y + 1 : y;

            return View(kpi);
        }

    }
}


// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using start.Models;      
using start.Data;           // Employee, EditEmployeeProfile
using start.Services;               // IEmployeeProfileService
using start.Models.ViewModels;
namespace start.Controllers
{
    
    [Route("Employee")] // /Employee/...
    public class EmployeeController : Controller
    {
        private readonly IEmployeeProfileService _svc;
        private readonly IScheduleService _s;
        private readonly IPayrollService _p;
        private readonly ApplicationDbContext _db;

        private readonly IDayOffService _dayoff;
        // Controllers/EmployeeController.cs (thêm ngay sau CurrentEmpId)
// CHỈNH
private string? CurrentRole =>
    (HttpContext.Session.GetString("RoleID") ??   // ưu tiên RoleID
     HttpContext.Session.GetString("Role"))       // fallback Role
    ?.Trim().ToUpperInvariant();

// CHO PHÉP: NV | EM | SL
private bool CanAccessDayOff() =>
    CurrentRole is "NV" or "EM" or "SL";

        public EmployeeController(ApplicationDbContext db, IEmployeeProfileService svc, IScheduleService s, IPayrollService p, IDayOffService dayoff)
        {
            _db = db;
            _svc = svc;
            _s = s;
            _p = p;
            _dayoff = dayoff;
        }
       

        // Lấy EmployeeID từ session
        private string? CurrentEmpId => HttpContext.Session.GetString("EmployeeID");

        // GET /Employee  (Hồ sơ)
        [HttpGet]
        public IActionResult Profile()
        {
            var id = HttpContext.Session.GetString("EmployeeID");
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Login", "Account");

            var emp = _db.Employees
                         .AsNoTracking()
                         .Include(e => e.Branch)
                         .SingleOrDefault(e => e.EmployeeID == id);

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
                ViewBag.ActiveMenu = "EditProfile";
                ViewData["Employee"] = _svc.GetById(CurrentEmpId!);
                return View("EditProfile", model);
            }

            var ok = _svc.EditProfile(CurrentEmpId!, model, out var error);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error);
                ViewBag.ActiveMenu = "EditProfile";
                ViewData["Employee"] = _svc.GetById(CurrentEmpId!);
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
            id ??= HttpContext.Session.GetString("EmployeeID");
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



    }
}

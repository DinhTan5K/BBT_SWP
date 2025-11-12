
// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
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
        private readonly IAttendanceService _attendanceService;
        // Controllers/EmployeeController.cs (thêm ngay sau CurrentEmpId)
// CHỈNH
private string? CurrentRole =>
    (HttpContext.Session.GetString("RoleID") ??   // ưu tiên RoleID
     HttpContext.Session.GetString("Role"))       // fallback Role
    ?.Trim().ToUpperInvariant();

// CHO PHÉP: NV | EM | SL
private bool CanAccessDayOff() =>
    CurrentRole is "SH" or "EM" or "SL";

        public EmployeeController(ApplicationDbContext db, IEmployeeProfileService svc, IScheduleService s, IPayrollService p, IDayOffService dayoff, IAttendanceService attendanceService)
        {
            _db = db;
            _svc = svc;
            _s = s;
            _p = p;
            _dayoff = dayoff;
            _attendanceService = attendanceService;
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
        public async Task<IActionResult> Schedule(string? id, int? month, int? year)
        {
            id ??= HttpContext.Session.GetString("EmployeeID");
            if (string.IsNullOrEmpty(id)) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;
            int m = month ?? today.Month;
            int y = year ?? today.Year;

            var dto = _s.GetMonthSchedule(id, m, y);

            // Lấy thông tin check-in hôm nay
           // Lấy thông tin check-in hôm nay
// Lấy thông tin check-in hôm nay
var tomorrow = today.AddDays(1);

var todayCheckIn = await _db.Attendances
    .AsNoTracking()
    .FirstOrDefaultAsync(a =>
        a.EmployeeID == id &&
        a.CheckInTime >= today &&
        a.CheckInTime < tomorrow);




            
            // Lấy ca làm việc hôm nay - query trực tiếp từ database (không phụ thuộc vào tháng được chọn)
            // Lấy tất cả ca của nhân viên và filter ở memory để debug
            var allSchedules = await _db.WorkSchedules
                .Where(w => w.EmployeeID == id)
                .ToListAsync();
            
            var todaySchedules = allSchedules
                .Where(w => w.WorkDate.Date == today.Date)
                .ToList();
            
            // Debug: Log để kiểm tra
            System.Diagnostics.Debug.WriteLine($"=== DEBUG CHECK-IN ===");
            System.Diagnostics.Debug.WriteLine($"EmployeeID: {id}, Today: {today:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine($"Tổng số ca: {allSchedules.Count}");
            foreach (var s in allSchedules.Take(5))
            {
                System.Diagnostics.Debug.WriteLine($"  Ca ID={s.WorkScheduleID}, Date={s.WorkDate:yyyy-MM-dd}, Shift={s.Shift}");
            }
            System.Diagnostics.Debug.WriteLine($"Số ca hôm nay: {todaySchedules.Count}");

            ViewBag.ActiveMenu = "Profile";
            ViewBag.ActiveTab = "schedule";
            ViewBag.TodayCheckIn = todayCheckIn;
            ViewBag.TodaySchedules = todaySchedules;
            ViewBag.EmployeeId = id;
            if (!string.IsNullOrEmpty(Request.Query["ok"]))
{
    TempData["ok"] = Request.Query["ok"].ToString();
}


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

        // GET: Check-in/Check-out Modal
     
[HttpGet("CheckIn/{workScheduleId?}")]
public async Task<IActionResult> CheckIn(int? workScheduleId)
{
    var empId = CurrentEmpId;
    if (string.IsNullOrEmpty(empId))
    return PartialView("_CheckInModal", new { canStart = false, message = "Vui lòng đăng nhập.", isCheckIn = true, workScheduleId });


    var emp = await _db.Employees.FindAsync(empId);
    if (emp == null)
        return PartialView("_CheckInModal", new { canStart = false, message = "Không tìm thấy nhân viên.", isCheckIn = true });

    if (string.IsNullOrEmpty(emp.AvatarUrl))
        return PartialView("_CheckInModal", new { canStart = false, message = "Bạn chưa có ảnh đại diện để nhận diện khuôn mặt. Vui lòng cập nhật trong Edit Profile.", isCheckIn = true });

    var today = DateTime.Today;

    WorkSchedule? schedule = null;
    if (workScheduleId.HasValue)
    {
        schedule = await _db.WorkSchedules
            .FirstOrDefaultAsync(w => w.WorkScheduleID == workScheduleId.Value && w.EmployeeID == empId);
    }
    if (schedule == null)
    {
        schedule = await _db.WorkSchedules
            .FirstOrDefaultAsync(w => w.EmployeeID == empId && w.WorkDate.Date == today);
    }

    if (schedule == null)
        return PartialView("_CheckInModal", new { canStart = false, message = $"Hôm nay ({today:dd/MM/yyyy}) bạn không có ca làm việc.", isCheckIn = true });

    // 🔽🔽🔽 Chính là 2 đoạn bạn hỏi ở đây 🔽🔽🔽
    var now = DateTime.Now;
    if (!ShiftTimeHelper.CanCheckIn(now, schedule.WorkDate, schedule.Shift, out var msg))
        return PartialView("_CheckInModal", new { canStart = false, message = msg, isCheckIn = true });

    var already = await _attendanceService.GetTodayCheckInAsync(empId);
    if (already != null)
        return PartialView("_CheckInModal", new { canStart = false, message = "Bạn đã check-in hôm nay. Vui lòng check-out trước.", isCheckIn = true });
    // 🔼🔼🔼 Hết 2 đoạn kiểm tra này 🔼🔼🔼

    // ✅ Nếu qua được hết mấy bước trên thì render modal có video
    return PartialView("_CheckInModal", new
    {
        canStart = true,
        message = "",
        isCheckIn = true,
        workScheduleId = schedule.WorkScheduleID
    });
}

[HttpGet("CheckOut/{workScheduleId?}")]
public async Task<IActionResult> CheckOut(int? workScheduleId)
{
    var empId = CurrentEmpId;
    if (string.IsNullOrEmpty(empId))
        return PartialView("_CheckInModal", new { canStart = false, message = "Vui lòng đăng nhập.", isCheckIn = false, workScheduleId });

    var checkIn = await _attendanceService.GetTodayCheckInAsync(empId);
    if (checkIn == null)
        return PartialView("_CheckInModal", new { canStart = false, message = "Bạn chưa check-in hôm nay.", isCheckIn = false, workScheduleId });

    if (checkIn.CheckOutTime != null)
        return PartialView("_CheckInModal", new { canStart = false, message = "Bạn đã check-out rồi.", isCheckIn = false, workScheduleId });

    // ✅ Lấy workScheduleId nếu null
    var wsId = workScheduleId ?? await _db.WorkSchedules
        .Where(w => w.EmployeeID == empId && w.WorkDate == DateTime.Today)
        .Select(w => (int?)w.WorkScheduleID)
        .FirstOrDefaultAsync();

    // ✅ Render modal cho Check-out
    return PartialView("_CheckInModal", new
    {
        canStart = true,
        message = "",
        isCheckIn = false,
        workScheduleId = wsId
    });
}




        // POST: Process Check-in
        [HttpPost("DoCheckIn")]
[IgnoreAntiforgeryToken]// Tạm thời bỏ qua để test, sau này có thể dùng [ValidateAntiForgeryToken] với cấu hình đúng
        public async Task<IActionResult> ProcessCheckIn([FromBody] CheckInRequest request)
        {
            var employeeId = CurrentEmpId;
            if (string.IsNullOrEmpty(employeeId))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrEmpty(request.ImageBase64))
                return Json(new { success = false, message = "Không có ảnh để xử lý." });

            var (success, message, attendance) = await _attendanceService.CheckInAsync(
                employeeId, 
                request.WorkScheduleId, 
                request.ImageBase64);

            if (success)
            {
                TempData["ok"] = message;
                return Json(new { success = true, message = message });
            }

            return Json(new { success = false, message = message });
        }

        // POST: Process Check-out
        [HttpPost("DoCheckOut")]
[IgnoreAntiforgeryToken] // Tạm thời bỏ qua để test
        public async Task<IActionResult> ProcessCheckOut([FromBody] CheckInRequest request)
        {
            var employeeId = CurrentEmpId;
            if (string.IsNullOrEmpty(employeeId))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrEmpty(request.ImageBase64))
                return Json(new { success = false, message = "Không có ảnh để xử lý." });

            var (success, message, attendance) = await _attendanceService.CheckOutAsync(
                employeeId, 
                request.ImageBase64);

            if (success)
            {
                TempData["ok"] = message;
                return Json(new { success = true, message = message });
            }

            return Json(new { success = false, message = message });
        }

        // POST: Upload Face Image
        [HttpPost("UploadFaceImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFaceImage(IFormFile faceImage)
        {
            var employeeId = CurrentEmpId;
            if (string.IsNullOrEmpty(employeeId))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            if (faceImage == null || faceImage.Length == 0)
                return Json(new { success = false, message = "File ảnh không hợp lệ." });

            var success = await _attendanceService.UploadFaceImageAsync(employeeId, faceImage);
            if (success)
            {
                TempData["ok"] = "Đã cập nhật ảnh khuôn mặt.";
                return Json(new { success = true, message = "Đã cập nhật ảnh khuôn mặt thành công." });
            }

            return Json(new { success = false, message = "Cập nhật ảnh khuôn mặt thất bại." });
        }

        // GET: Attendance History
        [HttpGet("Attendance")]
        public async Task<IActionResult> Attendance(string? id, DateTime? fromDate, DateTime? toDate)
        {
            id ??= CurrentEmpId;
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Login", "Account");

            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var history = await _attendanceService.GetAttendanceHistoryAsync(id, fromDate, toDate);
            ViewBag.ActiveMenu = "Profile";
            ViewBag.ActiveTab = "attendance";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View("Attendance", history);
        }
    }

    // Request models
    public class CheckInRequest
    {
        public int? WorkScheduleId { get; set; }
        public string? ImageBase64 { get; set; }
    }
}

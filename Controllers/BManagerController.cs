//dotnet add package ClosedXML (ae nho tai them thu vien nay ve nhe)
using System.Collections.Generic;
using start.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using start.Data;
using Microsoft.EntityFrameworkCore;
using start.Models;
using ClosedXML.Excel;
using System.IO;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using start.DTOs;
using System.Text.Json;

public class BranchManagerAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        // SỬA: Kiểm tra cả "RoleID" và "Role" để nhất quán với toàn bộ ứng dụng
        var roleValue = session.GetString("RoleID") ?? session.GetString("Role");
        var branchIdValue = session.GetString("BranchId");

        // --- START DEBUG LOGGING ---
        // Đoạn code này sẽ in ra cửa sổ Output/Debug của Visual Studio
        // Giúp chúng ta biết chính xác giá trị trong Session là gì.
        System.Diagnostics.Debug.WriteLine("--- BranchManagerAuthorize Check ---");
        System.Diagnostics.Debug.WriteLine($"Session Role (from RoleID/Role): {roleValue}");
        System.Diagnostics.Debug.WriteLine($"Session BranchId: {branchIdValue}");
        // --- END DEBUG LOGGING ---

        var isAjax = string.Equals(
            context.HttpContext.Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase
        );

        // Cho phép cả “BM” hoặc “BranchManager” để tương thích session khác nhau
        // SỬA: Mở rộng kiểm tra để chấp nhận cả "BRANCH MANAGER" (có dấu cách)
        var normalizedRole = roleValue?.Trim().Replace(" ", "").ToUpperInvariant();
        bool isValidRole = !string.IsNullOrEmpty(normalizedRole) && 
                           (normalizedRole == "BM" || 
                            normalizedRole == "BRANCHMANAGER");

        if (string.IsNullOrEmpty(branchIdValue) || !isValidRole)
        {
            if (isAjax)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ."
                })
                { StatusCode = 401 };
            }
            else
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }

        await Task.CompletedTask;
    }
}


[BranchManagerAuthorize]

public class BManagerController : Controller
{

    private readonly IDashboardService _dashboardService;
    private readonly IEmployeeManagementService _employeeService;
    private readonly IScheduleForManagerService _scheduleForManagerService;
    private readonly IReportService _reportService;
    private readonly IContractService _contractService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IPayrollService _payrollService;

    private readonly IDayOffService _dayOffService;


    public BManagerController(
        IDashboardService dashboardService,
        IEmployeeManagementService employeeService,
        IScheduleForManagerService scheduleService,
        IReportService reportService,
        IContractService contractService,
        IWebHostEnvironment webHostEnvironment,
        ApplicationDbContext context,
        IPaymentService paymentService,
        IPayrollService payrollService,
        IDayOffService dayOffService
    )
    {
        _dashboardService = dashboardService;
        _employeeService = employeeService;
        _scheduleForManagerService = scheduleService;
        _reportService = reportService;
        _contractService = contractService;
        _webHostEnvironment = webHostEnvironment;
        _context = context;
        _paymentService = paymentService;
        _payrollService = payrollService;
        _dayOffService = dayOffService;
    }

    private int? CurrentBranchId
    {
        get
        {
            var branchIdValue = HttpContext.Session.GetString("BranchId");
            if (int.TryParse(branchIdValue, out var branchId))
            {
                return branchId;
            }
            return null;
        }
    }

    // ------------------ DASHBOARD ------------------


    [HttpGet]
    public async Task<IActionResult> Index()
    {


        var managerName = HttpContext.Session.GetString("EmployeeName");
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var dashboardData = await _dashboardService.GetDashboardSummaryAsync(CurrentBranchId.Value);
        dashboardData.ManagerName = managerName ?? "Quản lý";

        // SỬA LỖI: Thêm logic lấy dữ liệu hiệu suất nhân viên và truyền qua ViewBag
        ViewBag.EmployeeSummary = await _dashboardService.GetWeeklyEmployeeSummaryAsync(CurrentBranchId.Value);

        return View(dashboardData);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTargets(int TargetOrders, decimal TargetRevenue)
    {
        if (TargetOrders <= 0 || TargetRevenue <= 0)
        {
            return BadRequest("Mục tiêu phải là số dương.");
        }
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var (success, message) = await _dashboardService.UpdateBranchTargetsAsync(CurrentBranchId.Value, TargetOrders, TargetRevenue);

        if (success)
        {
            return Ok(new { success = true, message = message });
        }

        return BadRequest(message);
    }



    // ------------------ EMPLOYEE CRUD ------------------

    public async Task<IActionResult> ViewEmp()
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var employees = await _employeeService.GetAllEmployeesByBranchAsync(CurrentBranchId.Value);
        return View(employees);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideEmp(string id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var (success, errorMessage) = await _employeeService.HideEmployeeAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return NotFound(errorMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreEmp(string id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var (success, errorMessage) = await _employeeService.RestoreEmployeeAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return NotFound(errorMessage);
    }

    [HttpGet]
public async Task<IActionResult> CreateEmpPartial()
{
    var roles = await _employeeService.GetSelectableRolesAsync();
    ViewBag.SelectableRoles = new SelectList(roles, "RoleID", "RoleName");
    
    // ⭐️ SỬA: Gửi Model Entity EmployeeBranchRequest
    var model = new EmployeeBranchRequest 
    { 
        BranchId = CurrentBranchId ?? 0,
        RequestType = RequestType.Add, // Đặt mặc định là Add
        Status = RequestStatus.Pending // Đặt mặc định là Pending
        // Các trường khác như RequestedBy sẽ được set trong POST
    };
    return PartialView("_CreateEmpPartial", model);
}

[HttpPost]
[ValidateAntiForgeryToken]
// ⭐️ SỬA: Nhận Model Entity EmployeeBranchRequest
public async Task<IActionResult> CreateEmp(EmployeeBranchRequest request) 
{
    var requestedById = HttpContext.Session.GetString("EmployeeID"); 
    var branchId = CurrentBranchId;

    if (string.IsNullOrEmpty(requestedById) || !branchId.HasValue)
    {
        return Unauthorized("Phiên đăng nhập hoặc chi nhánh không hợp lệ.");
    }
    
    // Cần xóa ModelState cho các trường sẽ được Controller/Service ghi đè (DB generated, FK, v.v.)
    ModelState.Remove(nameof(EmployeeBranchRequest.Id)); 
    ModelState.Remove(nameof(EmployeeBranchRequest.RequestedBy));
    ModelState.Remove(nameof(EmployeeBranchRequest.RequestedAt));
    ModelState.Remove(nameof(EmployeeBranchRequest.BranchId)); // Set lại ở dưới
    
    // Gán các giá trị không đến từ form
    request.RequestedBy = requestedById;
    request.BranchId = branchId.Value;
    request.RequestType = RequestType.Add; // Đảm bảo loại Request là Add
    request.Status = RequestStatus.Pending; // Đảm bảo trạng thái là Pending

    if (ModelState.IsValid)
    {
        // ⭐️ GỌI HÀM SERVICE MỚI
        // Phải thay đổi Service để chấp nhận EmployeeBranchRequest
        var (success, message) = await _employeeService.SubmitAddEmployeeRequestAsync(request); 

        if (success)
        {
            TempData["Success"] = message; 
            return Json(new { success = true, redirectUrl = Url.Action("ViewEmp") }); 
        }

        // Nếu Service trả về lỗi
        ModelState.AddModelError("", message);
    }

    // Trả lại Model Entity khi thất bại
    var roles = await _employeeService.GetSelectableRolesAsync();
    ViewBag.SelectableRoles = new SelectList(roles, "RoleID", "RoleName", request.RoleID);
    return PartialView("_CreateEmpPartial", request);
}
    [HttpGet]
    public async Task<IActionResult> EditEmp(string id)
    {

        var emp = await _employeeService.GetEmployeeByIdAsync(id);
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        if (emp == null || emp.BranchID != CurrentBranchId)
        {
            return NotFound("Không tìm thấy nhân viên hoặc bạn không có quyền truy cập.");
        }


        var roles = await _employeeService.GetSelectableRolesAsync();
        ViewBag.SelectableRoles = new SelectList(roles, "RoleID", "RoleName", emp.RoleID);

        return View(emp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmp(Employee emp)
    {
        ModelState.Remove("Role");


        if (ModelState.IsValid)
        {
            if (!CurrentBranchId.HasValue)
            {
                return NotFound("Branch ID not found");
            }

            var (success, errors) = await _employeeService.UpdateEmployeeAsync(emp, CurrentBranchId.Value);

            if (success)
            {
                return RedirectToAction("ViewEmp");
            }
            else
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }
        }

        ViewBag.SelectableRoles = new SelectList(await _employeeService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
        return View(emp);
    }


    // ------------------ WORK SCHEDULE ------------------

    [HttpGet]
    public async Task<IActionResult> WorkSchedule(DateTime? startDate, DateTime? endDate)
    {


        var today = DateTime.Today;
        var start = startDate ?? new DateTime(today.Year, today.Month, 1);
        var end = endDate ?? start.AddMonths(1).AddDays(-1);

        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var schedules = await _scheduleForManagerService.GetWorkScheduleAsync(CurrentBranchId.Value, start, end);

        ViewBag.CurrentDate = start;
        ViewBag.StartDate = start.ToString("yyyy-MM-dd");
        ViewBag.EndDate = end.ToString("yyyy-MM-dd");

        return View(schedules);
    }

    [HttpGet]

    public async Task<IActionResult> CreateSchedulePartial(DateTime? date)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var employees = await _scheduleForManagerService.GetActiveEmployeeAsync(CurrentBranchId.Value);


        ViewBag.Employees = new SelectList(
            employees ?? new List<Employee>(),
            "EmployeeID",
            "FullName"
        );


        var model = new WorkSchedule
        {
            Date = date?.Date ?? DateTime.Today
        };

        return PartialView("_CreateSchedulePartial", model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(WorkSchedule schedule)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).ToList();
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errors));
            return BadRequest(new { message = "Model invalid", errors });
        }
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        if (ModelState.IsValid)

        {

            var (success, errorMessage) = await _scheduleForManagerService.ManagerCreateScheduleAsync(schedule, CurrentBranchId.Value);

            if (success)
            {
                return Json(new { success = true });
            }
            ModelState.AddModelError("", errorMessage!);
        }


        var employees = await _scheduleForManagerService.GetActiveEmployeeAsync(CurrentBranchId.Value);
        ViewBag.Employees = new SelectList(
            employees ?? new List<Employee>(),
            "EmployeeID",
            "FullName",
            schedule.EmployeeID
        );


        if (schedule.Date.Year < 1900)
        {
            schedule.Date = DateTime.Today;
        }
        if (string.IsNullOrEmpty(schedule.Status))
        {
            schedule.Status = "Chưa duyệt";
        }



        return PartialView("_CreateSchedulePartial", schedule);
    }

    [HttpGet]

    public async Task<IActionResult> EditSchedulePartial(int id)
    {
        var branchId = CurrentBranchId; // Dùng property CurrentBranchId
        if (!branchId.HasValue)
            return Unauthorized("Không xác định được chi nhánh.");

        var schedule = await _scheduleForManagerService.GetScheduleByIdAsync(id);
        if (schedule == null)
            return NotFound("Không tìm thấy lịch làm việc.");


        if (schedule.EmployeeID != null)
        {
            var employee = await _context.Employees.FindAsync(schedule.EmployeeID);
            if (employee == null || employee.BranchID != branchId.Value)
                return Unauthorized("Bạn không có quyền chỉnh sửa ca làm này.");
        }

        // Gán lại SelectList cho ViewBag
        var employees = await _scheduleForManagerService.GetActiveEmployeeAsync(branchId.Value);
        ViewBag.Employees = new SelectList(
            employees ?? new List<Employee>(),
            "EmployeeID",
            "FullName",
            schedule.EmployeeID // Chọn Employee đang có trong schedule
        );


        ViewBag.Shifts = new SelectList(new List<string> { "Morning", "Night" }, schedule.Shift);

        return PartialView("_EditSchedulePartial", schedule);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(WorkSchedule schedule)
    {
        var isAjax = HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        var branchId = CurrentBranchId;

        if (!branchId.HasValue)
        {
            if (isAjax) return Json(new { success = false, errorMessage = "Phiên đăng nhập đã hết hạn." });
            return Unauthorized();
        }


        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _scheduleForManagerService.UpdateScheduleAsync(schedule, branchId.Value);

            if (success)
            {

                return Json(new { success = true });
            }


            ModelState.AddModelError("Date", errorMessage!);
        }


        var employees = await _scheduleForManagerService.GetActiveEmployeeAsync(branchId.Value);
        ViewBag.Employees = new SelectList(
            employees ?? new List<Employee>(),
            "EmployeeID",
            "FullName",
            schedule.EmployeeID
        );
        ViewBag.Shifts = new SelectList(new List<string> { "Morning", "Night" }, schedule.Shift);
        return PartialView("_EditSchedulePartial", schedule);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideSchedule(int id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var (success, message) = await _scheduleForManagerService.HideScheduleAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return BadRequest(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreSchedule(int id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var (success, message) = await _scheduleForManagerService.RestoreScheduleAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return BadRequest(message);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSchedule(int id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var (success, message) = await _scheduleForManagerService.ApproveScheduleAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return BadRequest(message);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectSchedule(int id)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var (success, message) = await _scheduleForManagerService.RejectScheduleAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return BadRequest(message);
    }


    [HttpGet]
    public async Task<IActionResult> GetScheduleDetailsForDate(DateTime date)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var schedules = await _scheduleForManagerService.GetScheduleDetailsForDateAsync(CurrentBranchId.Value, date);

        return PartialView("_ScheduleDetailsModal", schedules); // Đảm bảo trả về đúng Partial View
    }

    // BManagerController.cs

    // ------------------ SALARY REPORT ------------------

    [HttpGet]
    public async Task<IActionResult> SalaryReport(string? name, int month = 0, int year = 0)
    {
        (month, year) = NormalizeMonthYear(month, year);
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        // Logic này nên được chuyển vào ReportService.cs
        // Lấy dữ liệu đã chốt từ bảng Salaries để lấy FinalizationCount
        var finalizedSalaries = await _context.Salaries
            .Where(s => s.SalaryMonth.Year == year && s.SalaryMonth.Month == month && s.Employee.BranchID == CurrentBranchId.Value)
            .ToDictionaryAsync(s => s.EmployeeID);

        var salaries = await _reportService.GetSalaryReportAsync(name, month, year, CurrentBranchId.Value);

        ViewBag.Name = name;
        ViewBag.Month = month;
        ViewBag.Year = year;

        // Gộp dữ liệu: Cập nhật FinalizationCount và IsFinalized từ dữ liệu đã chốt
        foreach (var report in salaries)
        {
            if (finalizedSalaries.TryGetValue(report.EmployeeID, out var finalizedSalary))
            {
                report.FinalizationCount = finalizedSalary.FinalizationCount;
                report.IsFinalized = (finalizedSalary.Status == "Đã chốt");
            }
        }

        return View("SalaryReport", salaries);
    }


    // Trong BManagerController.cs

    [HttpGet]
    public async Task<IActionResult> ExportSalaryToExcel(string? name, int month = 0, int year = 0)
    {
        (month, year) = NormalizeMonthYear(month, year);
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        // Lấy báo cáo tổng hợp (Sheet 1)
        var salaries = await _reportService.GetSalaryReportAsync(name, month, year, CurrentBranchId.Value);

        // Lấy tất cả chi tiết ca làm việc trong tháng (Sử dụng hàm đã thêm vào IReportService)
        var allWorkSchedules = await _reportService.GetAllWorkSchedulesForMonthAsync(month, year, CurrentBranchId.Value);

        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();

            // ===========================================
            // SHEET 1: TỔNG HỢP LƯƠNG
            // ===========================================
            var wsSummary = workbook.Worksheets.Add($"1. Summary {month}-{year}");

            // Headers
            wsSummary.Cell(1, 1).Value = "Employee ID";
            wsSummary.Cell(1, 2).Value = "Full Name";
            wsSummary.Cell(1, 3).Value = "Total Shifts";
            wsSummary.Cell(1, 4).Value = "Total Hours";
            wsSummary.Cell(1, 5).Value = "Lương CB (100%)";
            wsSummary.Cell(1, 7).Value = "Thưởng";
            wsSummary.Cell(1, 8).Value = "Phạt/Khấu trừ";
            wsSummary.Cell(1, 9).Value = "Total Salary (VND)";

            wsSummary.Range("A1:I1").Style.Font.Bold = true;
            wsSummary.Range("A1:I1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Data
            int row = 2;
            foreach (var s in salaries)
            {
                wsSummary.Cell(row, 1).Value = s.EmployeeID;
                wsSummary.Cell(row, 2).Value = s.FullName;
                wsSummary.Cell(row, 3).Value = s.TotalShifts;
                wsSummary.Cell(row, 4).Value = s.TotalHours;
                wsSummary.Cell(row, 5).Value = s.BaseSalary;
                wsSummary.Cell(row, 7).Value = s.Bonus;
                wsSummary.Cell(row, 8).Value = s.Penalty;
                wsSummary.Cell(row, 9).Value = s.TotalSalary;
                row++;
            }
            wsSummary.Columns().AdjustToContents();


            // ===========================================
            // SHEET 2: CHI TIẾT CA LÀM THEO NGÀY
            // ===========================================
            var wsDetail = workbook.Worksheets.Add($"2. Detail Shifts {month}-{year}");
            int detailRow = 1;

            // Headers
            wsDetail.Cell(detailRow, 1).Value = "Employee ID";
            wsDetail.Cell(detailRow, 2).Value = "Full Name";
            wsDetail.Cell(detailRow, 3).Value = "Date";
            wsDetail.Cell(detailRow, 4).Value = "Shift";
            wsDetail.Cell(detailRow, 5).Value = "Check In";
            wsDetail.Cell(detailRow, 6).Value = "Check Out";
            wsDetail.Cell(detailRow, 7).Value = "Duration (Hours)";
            wsDetail.Cell(detailRow, 8).Value = "Base Rate (VND/h)";
            wsDetail.Cell(detailRow, 9).Value = "Total Pay (VND)";

            wsDetail.Range("A1:I1").Style.Font.Bold = true;
            wsDetail.Range("A1:I1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
            detailRow++;

            // Data (Duyệt qua tất cả Attendance đã lấy)
            foreach (var schedule in allWorkSchedules)
            {
                if (schedule.CheckInTime.HasValue && schedule.CheckOutTime.HasValue)
                {
                    // 1. Lấy lương giờ từ hợp đồng (Cần đảm bảo _contractService được inject)
                    var contract = await _contractService.GetActiveHourlyContractOnDateAsync(schedule.EmployeeID, schedule.Date);
                    decimal hourlyRate = (contract != null && contract.PaymentType == Contract.PaymentTypes.Gio)
                                                ? contract.BaseRate
                                                : 0m;

                    double durationHours = (schedule.CheckOutTime.Value - schedule.CheckInTime.Value).TotalHours;
                    decimal totalShiftPay = (decimal)durationHours * hourlyRate;

                    wsDetail.Cell(detailRow, 1).Value = schedule.EmployeeID;
                    wsDetail.Cell(detailRow, 2).Value = schedule.Employee?.FullName ?? "N/A";
                    wsDetail.Cell(detailRow, 3).Value = schedule.Date.ToString("yyyy-MM-dd");
                    wsDetail.Cell(detailRow, 4).Value = schedule.Shift;
                    wsDetail.Cell(detailRow, 5).Value = schedule.CheckInTime?.ToString("HH:mm");
                    wsDetail.Cell(detailRow, 6).Value = schedule.CheckOutTime?.ToString("HH:mm");
                    wsDetail.Cell(detailRow, 7).Value = durationHours;
                    wsDetail.Cell(detailRow, 8).Value = hourlyRate;
                    wsDetail.Cell(detailRow, 9).Value = totalShiftPay;
                    detailRow++;
                }
            }

            wsDetail.Columns().AdjustToContents();
            wsDetail.Range(2, 8, detailRow - 1, 8).Style.NumberFormat.Format = "#,##0";
            wsDetail.Range(2, 9, detailRow - 1, 9).Style.NumberFormat.Format = "#,##0";


            // --- TRẢ VỀ FILE ---
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"SalaryReport_{month}_{year}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Export failed: {ex.Message}");
        }

    }
    private (int, int) NormalizeMonthYear(int month, int year)
    {
        if (month <= 0 || month > 12)
            month = DateTime.Now.Month;
        if (year <= 0)
            year = DateTime.Now.Year;
        return (month, year);
    }

    [HttpGet]
public async Task<IActionResult> GetEmployeeShiftsPartial(string employeeId, int month, int year)
{
    if (!CurrentBranchId.HasValue)
    {
        return BadRequest("Branch ID not found");
    }

    var shifts = await _reportService.GetDetailedWorkSchedulesAsync(employeeId, month, year, CurrentBranchId.Value);

    // _EmployeeShiftsPartial là Partial View mới cần được tạo
    return PartialView("_EmployeeShiftsPartial", shifts);
}


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeSalary(string employeeId, int month, int year)
    {
        if (!CurrentBranchId.HasValue)
            return Unauthorized();


        var (success, message) = await _payrollService.CalculateAndFinalizeSalaryAsync(
            employeeId, year, month, CurrentBranchId.Value);

        if (success)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }


        return RedirectToAction(nameof(SalaryReport), new { month = month, year = year });
    }

    // ------------------ REVENUE REPORT ------------------



    [HttpGet]
    public async Task<IActionResult> RevenueReport(string month)
    {
        DateTime startOfMonth;
        DateTime endOfMonth;

        if (string.IsNullOrEmpty(month))
        {
            // Mặc định là tháng hiện tại
            var today = DateTime.Today;
            startOfMonth = new DateTime(today.Year, today.Month, 1);
            month = today.ToString("yyyy-MM"); // Gán giá trị "yyyy-MM"
        }
        else
        {
            // Parse chuỗi "yyyy-MM"
            if (DateTime.TryParse(month + "-01", out var parsedDate))
            {
                startOfMonth = new DateTime(parsedDate.Year, parsedDate.Month, 1);
            }
            else
            {
                // Nếu parse lỗi, quay về tháng hiện tại
                var today = DateTime.Today;
                startOfMonth = new DateTime(today.Year, today.Month, 1);
                month = today.ToString("yyyy-MM");
            }
        }

        // Tính ngày cuối cùng của tháng
        endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // === 1. Lấy dữ liệu tháng HIỆN TẠI ===
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }
        var reportData = await _reportService.GetRevenueReportAsync(CurrentBranchId.Value, startOfMonth, endOfMonth);

        // === 2. Tính toán các KPI của tháng HIỆN TẠI ===
        decimal totalRevenue = reportData.Sum(r => r.TotalRevenue);
        int totalOrders = reportData.Sum(r => r.TotalOrders);
        decimal averageOrderValue = (totalOrders > 0) ? (totalRevenue / totalOrders) : 0;

        // === 3. Lấy dữ liệu tháng TRƯỚC để tính tăng trưởng ===
        DateTime prevMonthStart = startOfMonth.AddMonths(-1);
        DateTime prevMonthEnd = startOfMonth.AddDays(-1); // Ngày cuối cùng của tháng trước

        var prevMonthReportData = await _reportService.GetRevenueReportAsync(CurrentBranchId.Value, prevMonthStart, prevMonthEnd);
        decimal prevMonthRevenue = prevMonthReportData.Sum(r => r.TotalRevenue);

        // === 4. Tính toán % tăng trưởng ===
        double growthPercentage = 0.0;
        if (prevMonthRevenue > 0)
        {
            // Công thức: (Hiện tại - Trước đó) / Trước đó
            growthPercentage = (double)((totalRevenue - prevMonthRevenue) / prevMonthRevenue);
        }
        else if (totalRevenue > 0)
        {
            // Tăng trưởng từ 0 lên số dương -> 100%
            growthPercentage = 1.0;
        }
        // Nếu cả 2 đều là 0, growthPercentage = 0.0 (đã khởi tạo)

        // === 5. Gửi TẤT CẢ 4 SỐ LIỆU KPI qua ViewBag ===
        ViewBag.SelectedMonth = month;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.AverageOrderValue = averageOrderValue;
        ViewBag.GrowthPercentage = growthPercentage;

        // (ViewBag.TotalPeriodRevenue không còn cần thiết vì đã có ViewBag.TotalRevenue)

        // Trả về Model (reportData) để view vẽ biểu đồ và danh sách chi tiết
        return View(reportData);
    }

    [HttpGet]
    public async Task<IActionResult> GetDailyOrderDetails(DateTime date)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var orders = await _reportService.GetOrdersByDateAsync(CurrentBranchId.Value, date);

        return PartialView("_DailyOrderDetails", orders);
    }

    // ------------------ Contracts ------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelContract(int contractId)
        {
            if (!CurrentBranchId.HasValue)
            {
                return Json(new { success = false, message = "Branch ID not found" });
            }

            var (success, message) = await _contractService.CancelContractAsync(contractId, CurrentBranchId.Value);
            return Json(new { success, message });
        }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeContractsPartial(string employeeId)
    {
        if (!CurrentBranchId.HasValue)
        {
            return NotFound("Branch ID not found");
        }

        var contracts = await _contractService.GetContractsByEmployeeIdAsync(employeeId, CurrentBranchId.Value);


        var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
        ViewBag.EmployeeNameForModal = employee?.FullName ?? "Employee";

        return PartialView("_EmployeeContractsPartial", contracts);
    }


    [HttpGet]
    public async Task<IActionResult> CreateContractPartial(string employeeId)
    {

        var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
        if (employee == null || employee.BranchID != CurrentBranchId)
        {
            return Content("Employee not found or invalid access.");
        }
        ViewBag.EmployeeNameForModal = employee.FullName;

        var newContract = new Contract
        {
            EmployeeId = employeeId,
            StartDate = DateTime.Today
        };
        return PartialView("_CreateContractPartial", newContract);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContract(Contract contract)
    {

        ModelState.Remove("Status");
        ModelState.Remove("Employee");

        if (ModelState.IsValid)
        {
            if (!CurrentBranchId.HasValue)
            {
                return NotFound("Branch ID not found");
            }

            var (success, message) = await _contractService.CreateContractAsync(contract, CurrentBranchId.Value);
            if (success)
            {
                return Json(new { success = true, employeeId = contract.EmployeeId });
            }
            ModelState.AddModelError("", message);
        }


        var employee = await _employeeService.GetEmployeeByIdAsync(contract.EmployeeId);
        ViewBag.EmployeeNameForModal = employee?.FullName ?? "Employee";

        return PartialView("_CreateContractPartial", contract);
    }

    


    // ------------------Refund Request ------------------

    [HttpGet("RefundRequests")]
    public async Task<IActionResult> RefundRequests()
    {
        var pendingRefunds = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.Status == "Chờ hoàn tiền")
            .OrderByDescending(o => o.RefundAt)
            .ToListAsync();

        return View(pendingRefunds);
    }


    [HttpPost("ApproveRefund/{orderId}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveRefund(int orderId)
{
    var branchId = HttpContext.Session.GetInt32("BranchId");

    if (!branchId.HasValue)
    {
        TempData["Error"] = "Phiên đăng nhập hết hạn hoặc thông tin chi nhánh không hợp lệ.";
        return RedirectToAction(nameof(RefundRequests));
    }

    // 1. Lấy đơn hàng và kiểm tra điều kiện
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.OrderID == orderId && o.BranchID == branchId.Value);

    if (order == null || order.Status != "Chờ hoàn tiền" || string.IsNullOrEmpty(order.TransId))
    {
        TempData["Error"] = "Đơn hàng không tồn tại, không có quyền truy cập, hoặc không đủ điều kiện hoàn tiền.";
        return RedirectToAction(nameof(RefundRequests));
    }

    // 2. Gọi API Hoàn tiền MoMo
    string description = $"Hoàn tiền đơn hàng {order.OrderCode} từ chi nhánh {branchId.Value}";
    
    // Sử dụng _paymentService đã inject
    string momoResponseJson = await _paymentService.RefundAsync(
        order.TransId!, 
        order.Total, // Giả sử hoàn tiền toàn bộ
        description
    );
    
    string momoResultMessage = "Lỗi không xác định từ MoMo.";
    bool refundSuccess = false;

    // 3. Phân tích phản hồi MoMo
    try
    {
        using var doc = JsonDocument.Parse(momoResponseJson);
        var root = doc.RootElement;
        
        // MoMo trả về resultCode là 0 nếu thành công
        if (root.TryGetProperty("resultCode", out var resultCodeElement) && resultCodeElement.GetInt32() == 0)
        {
            refundSuccess = true;
            momoResultMessage = "Hoàn tiền thành công. Trạng thái đơn hàng đã được cập nhật.";
        }
        else
        {
            // Lấy thông báo lỗi cụ thể từ MoMo
            if (root.TryGetProperty("message", out var messageElement))
            {
                momoResultMessage = $"Hoàn tiền thất bại. Lỗi MoMo: {messageElement.GetString()}";
            }
        }
    }
    catch (JsonException)
    {
        momoResultMessage = "Lỗi phân tích phản hồi JSON từ MoMo.";
    }

    // 4. Cập nhật Database
    if (refundSuccess)
    {
        order.Status = "Đã hoàn tiền";
        order.Status = "Đã hoàn tiền";
        TempData["Success"] = momoResultMessage;
    }
    else
    {
        // Giữ nguyên trạng thái "Chờ hoàn tiền" hoặc đổi thành "Hoàn tiền thất bại"
        order.Status = "Hoàn tiền thất bại"; 
        TempData["Error"] = momoResultMessage;
    }

    await _context.SaveChangesAsync();
    
    return RedirectToAction(nameof(RefundRequests));
}

    [HttpPost("RejectRefund/{orderId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRefund(int orderId)
    {
        var branchId = HttpContext.Session.GetInt32("BranchId");
        if (!branchId.HasValue)
        {
            TempData["Error"] = "Phiên đăng nhập hết hạn hoặc thông tin chi nhánh không hợp lệ.";
            return RedirectToAction(nameof(RefundRequests));
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId && o.BranchID == branchId.Value);

        if (order == null || order.Status != "Chờ hoàn tiền")
        {
            TempData["Error"] = "Đơn hàng không hợp lệ hoặc đã được xử lý.";
            return RedirectToAction(nameof(RefundRequests));
        }

        order.Status = "Từ chối hoàn tiền"; // Cập nhật trạng thái mới
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã từ chối yêu cầu hoàn tiền cho đơn hàng {order.OrderCode}.";
        return RedirectToAction(nameof(RefundRequests));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDetailsForRefund(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails).ThenInclude(od => od.ProductSize)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);

        if (order == null) return NotFound();

        return PartialView("_RefundOrderDetailsPartial", order);
    }

    // ------------------ Day Off Request ------------------
    [HttpGet]
    public async Task<IActionResult> DayOffManager()
    {
        if (!CurrentBranchId.HasValue)
            return Unauthorized();

        var requests = await _dayOffService.GetPendingByBranchAsync(CurrentBranchId.Value);

        return View(requests); // Trả về List<DayOffManagerVm>
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleDayOff(int requestId, string action) // action là 'approve' hoặc 'reject'
    {
        if (!CurrentBranchId.HasValue)
            return Unauthorized();

        string newStatus = (action == "approve") ? "Approved" :
                           (action == "reject") ? "Rejected" : "";

        if (string.IsNullOrEmpty(newStatus))
            return BadRequest("Hành động không hợp lệ.");

        var (success, message) = await _dayOffService.UpdateStatusAsync(
            requestId, CurrentBranchId.Value, newStatus);

        TempData[success ? "Success" : "Error"] = message;

        return RedirectToAction(nameof(DayOffManager));
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> HandleDayOffAjax(int requestId, string action)
{
    if (!CurrentBranchId.HasValue)
        return Json(new { success = false, message = "Phiên làm việc không hợp lệ." });

    string newStatus = (action == "approve") ? "Approved" :
                       (action == "reject") ? "Rejected" : "";

    if (string.IsNullOrEmpty(newStatus))
        return Json(new { success = false, message = "Hành động không hợp lệ." });

    var (success, message) = await _dayOffService.UpdateStatusAsync(
        requestId, CurrentBranchId.Value, newStatus);

    return Json(new { success, message });
}

}
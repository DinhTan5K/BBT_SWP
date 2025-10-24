//dotnet add package ClosedXML (ae nho tai them thu vien nay ve nhe)
using System.Collections.Generic;
using start.Services;
using Microsoft.AspNetCore.Mvc;
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

public class BManagerController : Controller
{
    private readonly IBManagerService _bManagerService;
    private const double RatePerHour = 25000;
    private const double HoursPerShift = 6;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public BManagerController(IBManagerService bManagerService, IWebHostEnvironment webHostEnvironment)
    {
        _bManagerService = bManagerService;
        _webHostEnvironment = webHostEnvironment;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branchIdValue = HttpContext.Session.GetString("BranchId");
        if (string.IsNullOrEmpty(branchIdValue) || !int.TryParse(branchIdValue, out var branchId))
        {
            return Unauthorized("Không thể xác định chi nhánh của bạn. Vui lòng đăng nhập lại.");
        }

        var managerName = HttpContext.Session.GetString("EmployeeName");
        var dashboardData = await _bManagerService.GetDashboardSummaryAsync(branchId);
        dashboardData.ManagerName = managerName ?? "Quản lý";

        return View(dashboardData);
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

    // ------------------ EMPLOYEE CRUD ------------------

    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> ViewEmp()
    {
        if (CurrentBranchId == null)
        {
            return Unauthorized("Không thể xác định chi nhánh của bạn.");
        }

        // SỬA: Gọi phiên bản async
        var employees = await _bManagerService.GetAllEmployeesByBranchAsync(CurrentBranchId.Value);
        return View(employees);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideEmp(string id)
    {
        if (CurrentBranchId == null) return Unauthorized();
        
        var (success, errorMessage) = await _bManagerService.HideEmployeeAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return NotFound(errorMessage); 
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreEmp(string id)
    {
        if (CurrentBranchId == null) return Unauthorized();
        
        var (success, errorMessage) = await _bManagerService.RestoreEmployeeAsync(id, CurrentBranchId.Value);

        if (success) return Ok(new { success = true });
        return NotFound(errorMessage); 
    }

    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> CreateEmpPartial()
    {
        // SỬA: Gọi phiên bản async
        var roles = await _bManagerService.GetSelectableRolesAsync();
        ViewBag.SelectableRoles = new SelectList(roles, "RoleID", "RoleName");
        return PartialView("_CreateEmpPartial", new Employee());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmp(Employee emp)
    {
        ModelState.Remove("Role");

        var branchIdValue = HttpContext.Session.GetString("BranchId");
        if (string.IsNullOrEmpty(branchIdValue) || !int.TryParse(branchIdValue, out var managerBranchId))
        {
            ModelState.AddModelError("", "Không thể xác định chi nhánh của bạn. Vui lòng đăng nhập lại.");
            // SỬA: Gọi phiên bản async
            ViewBag.SelectableRoles = new SelectList(await _bManagerService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
            return PartialView("_CreateEmpPartial", emp);
        }

        if (ModelState.IsValid)
        {
            var (newEmp, errorMessage) = await _bManagerService.CreateEmployeeAsync(emp, managerBranchId);

            if (errorMessage != null)
            {
                if (errorMessage.Contains("Số điện thoại") || errorMessage.Contains("phone number"))
                {
                    ModelState.AddModelError("PhoneNumber", errorMessage);
                }
                else if (errorMessage.Contains("Email"))
                {
                    ModelState.AddModelError("Email", errorMessage);
                }
                else
                {
                    ModelState.AddModelError("", errorMessage);
                }
                // SỬA: Gọi phiên bản async
                ViewBag.SelectableRoles = new SelectList(await _bManagerService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
                return PartialView("_CreateEmpPartial", emp);
            }

            return Json(new { success = true });
        }

        // SỬA: Gọi phiên bản async
        ViewBag.SelectableRoles = new SelectList(await _bManagerService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
        return PartialView("_CreateEmpPartial", emp);
    }

    [HttpGet]
    public async Task<IActionResult> EditEmp(string id)
    {
        var emp = await _bManagerService.GetEmployeeByIdAsync(id);

        if (emp == null || emp.BranchID != CurrentBranchId)
        {
            return NotFound("Không tìm thấy nhân viên hoặc bạn không có quyền truy cập.");
        }

        // SỬA: Gọi phiên bản async
        var roles = await _bManagerService.GetSelectableRolesAsync();
        ViewBag.SelectableRoles = new SelectList(roles, "RoleID", "RoleName", emp.RoleID);

        return View(emp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmp(Employee emp)
    {
        ModelState.Remove("Role");
        if (CurrentBranchId == null)
        {
            ModelState.AddModelError("", "Không thể xác định chi nhánh của bạn.");
            // SỬA: Gọi phiên bản async (bạn đã gọi đúng tên nhưng thiếu 'await')
            ViewBag.SelectableRoles = new SelectList(await _bManagerService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
            return View(emp);
        }

        if (ModelState.IsValid)
        {
            var (success, errors) = await _bManagerService.UpdateEmployeeAsync(emp, CurrentBranchId.Value);

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
        // SỬA: Gọi phiên bản async
        ViewBag.SelectableRoles = new SelectList(await _bManagerService.GetSelectableRolesAsync(), "RoleID", "RoleName", emp.RoleID);
        return View(emp);
    }

    // ------------------ PRODUCT CRUD ------------------

    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> ViewProduct()
    {
        // SỬA: Gọi phiên bản async
        var products = await _bManagerService.GetAllProductsWithSizesAsync();
        return View(products);
    }

    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> CreateProduct()
    {
        // SỬA: Gọi phiên bản async
        ViewBag.Categories = await _bManagerService.GetProductCategoriesAsync();
        return View();
    }


    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> CreateProductPartial()
    {
        // SỬA: Gọi phiên bản async
        ViewBag.Categories = new SelectList(await _bManagerService.GetProductCategoriesAsync(), "CategoryID", "CategoryName");
        return PartialView("_CreateProductPartial", new Product());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        product.ProductSizes = product.ProductSizes?
            .Where(ps => !string.IsNullOrEmpty(ps.Size) && ps.Price > 0)
            .ToList() ?? new List<ProductSize>();

        if (ModelState.IsValid)
        {
            var (success, errors) = await _bManagerService.CreateProductAsync(product);

            if (success)
            {
                return Json(new { success = true });
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }
        }

        // SỬA: Gọi phiên bản async
        ViewBag.Categories = new SelectList(await _bManagerService.GetProductCategoriesAsync(), "CategoryID", "CategoryName");
        return PartialView("_CreateProductPartial", product);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var (product, categories) = await _bManagerService.GetProductForEditAsync(id);

        if (product == null) return NotFound();

        ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product)
    {
        if (ModelState.IsValid)
        {
            var (success, errors) = await _bManagerService.UpdateProductAsync(product);

            if (success)
            {
                return RedirectToAction("ViewProduct");
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }
        }
        
        var (p, categories) = await _bManagerService.GetProductForEditAsync(product.ProductID);
        ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideProduct(int id)
    {
        await _bManagerService.HideProductAsync(id);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        await _bManagerService.RestoreProductAsync(id);
        return Ok();
    }

    // ------------------ WORK SCHEDULE ------------------


    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> WorkSchedule(DateTime? startDate, DateTime? endDate)
    {
        var branchIdValue = HttpContext.Session.GetString("BranchId");
        if (string.IsNullOrEmpty(branchIdValue) || !int.TryParse(branchIdValue, out var branchId))
        {
            return Unauthorized("Không thể xác định chi nhánh của bạn.");
        }

        // SỬA: Gọi phiên bản async
        var schedules = await _bManagerService.GetWorkSchedulesAsync(branchId, startDate, endDate);

        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(schedules);
    }


    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> CreateSchedulePartial()
    {
        if (CurrentBranchId == null)
        {
            return Unauthorized("Không thể xác định chi nhánh của bạn.");
        }
        // SỬA: Gọi phiên bản async (Đây là lỗi bạn báo cáo trước đó)
        ViewBag.Employees = await _bManagerService.GetActiveEmployeesAsync(CurrentBranchId.Value);
        return PartialView("_CreateSchedulePartial", new WorkSchedule());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(WorkSchedule schedule)
    {
        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _bManagerService.CreateScheduleAsync(schedule);
            if (success)
            {
                return Json(new { success = true });
            }
            ModelState.AddModelError("", errorMessage!);
        }

        // SỬA: Gọi phiên bản async
        ViewBag.Employees = await _bManagerService.GetActiveEmployeesAsync(CurrentBranchId.Value);
        return PartialView("_CreateSchedulePartial", schedule);
    }

    [HttpGet]
    public async Task<IActionResult> EditSchedule(int id)
    {
        var schedule = await _bManagerService.GetScheduleByIdAsync(id);
        if (schedule == null) return NotFound();

        // SỬA: Gọi phiên bản async
        ViewBag.Employees = await _bManagerService.GetActiveEmployeesAsync(CurrentBranchId.Value);
        return View(schedule);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(WorkSchedule schedule)
    {
        if (ModelState.IsValid)
        {
            var (success, errorMessage) = await _bManagerService.UpdateScheduleAsync(schedule);

            if (success)
            {
                return RedirectToAction("WorkSchedule");
            }
            
            ModelState.AddModelError("Date", errorMessage!);
        }

        // SỬA: Gọi phiên bản async
        ViewBag.Employees = await _bManagerService.GetActiveEmployeesAsync(CurrentBranchId.Value);
        return View(schedule);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideSchedule(int id)
    {
        await _bManagerService.HideScheduleAsync(id);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreSchedule(int id)
    {
        await _bManagerService.RestoreScheduleAsync(id);
        return Ok();
    }
    
    // ------------------ SALARY REPORT ------------------

    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> SalaryReport(string? name, int month = 0, int year = 0)
    {
        if (month == 0) month = DateTime.Now.Month;
        if (year == 0) year = DateTime.Now.Year;

        // SỬA: Gọi phiên bản async
        var salaries = await _bManagerService.GetSalaryReportAsync(name, month, year, RatePerHour, HoursPerShift);

        ViewBag.Name = name;
        ViewBag.Month = month;
        ViewBag.Year = year;

        return View("SalaryReport", salaries);
    }


    [HttpGet]
    // SỬA: Chuyển sang async Task
    public async Task<IActionResult> ExportSalaryToExcel(int month = 0, int year = 0)
    {
        if (month == 0) month = DateTime.Now.Month;
        if (year == 0) year = DateTime.Now.Year;

        // SỬA: Gọi phiên bản async
        var salaries = await _bManagerService.GetSalaryReportAsync(null, month, year, RatePerHour, HoursPerShift);
        
        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Salary Report");
            ws.Cell(1, 1).Value = "Employee ID";
            ws.Cell(1, 2).Value = "Full Name";
            ws.Cell(1, 3).Value = "Total Shifts";
            ws.Cell(1, 4).Value = "Total Hours";
            ws.Cell(1, 5).Value = "Total Salary (VND)";

            int row = 2;
            foreach (var s in salaries)
            {
                ws.Cell(row, 1).Value = s.EmployeeID;
                ws.Cell(row, 2).Value = s.FullName;
                ws.Cell(row, 3).Value = s.TotalShifts;
                ws.Cell(row, 4).Value = s.TotalHours;
                ws.Cell(row, 5).Value = s.TotalSalary;
                row++;
            }

            ws.Range("A1:E1").Style.Font.Bold = true;
            ws.Range("A1:E1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            ws.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"SalaryReport_{month}_{year}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                
                // Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\""; // Cách cũ

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName); // Cách trả về file khuyến nghị
            }
        }
    }


    // ------------------ REVENUE REPORT ------------------

    [HttpGet]
    public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate)
    {
        var branchIdValue = HttpContext.Session.GetString("BranchId");
        if (string.IsNullOrEmpty(branchIdValue) || !int.TryParse(branchIdValue, out var managerBranchId))
        {
            return Unauthorized("Không thể xác định chi nhánh của bạn. Vui lòng đăng nhập lại.");
        }
        
        var start = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var end = endDate ?? DateTime.Now;

        var reportData = await _bManagerService.GetRevenueReportAsync(managerBranchId, start, end);
        
        ViewBag.StartDate = start.ToString("yyyy-MM-dd");
        ViewBag.EndDate = end.ToString("yyyy-MM-dd");
        ViewBag.TotalPeriodRevenue = reportData.Sum(r => r.TotalRevenue);

        return View(reportData);
    }

    // ------------------ Contracts ------------------

    [HttpGet]
    public async Task<IActionResult> GetEmployeeContractsPartial(string employeeId)
    {
        if (CurrentBranchId == null) return Unauthorized();

        var contracts = await _bManagerService.GetContractsByEmployeeIdAsync(employeeId, CurrentBranchId.Value);
        var employee = await _bManagerService.GetEmployeeByIdAsync(employeeId);
        ViewBag.EmployeeName = employee?.FullName ?? "Employee";

        return PartialView("_EmployeeContractsPartial", contracts);
    }


    [HttpGet]
    public async Task<IActionResult> CreateContractPartial(string employeeId)
    {
        if (CurrentBranchId == null) return Unauthorized();
        
        var employee = await _bManagerService.GetEmployeeByIdAsync(employeeId);
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
        if (CurrentBranchId == null) return Unauthorized();
        
        ModelState.Remove("Employee");

        if (ModelState.IsValid)
        {
            var (success, message) = await _bManagerService.CreateContractAsync(contract, CurrentBranchId.Value);
            if (success)
            {
                return Json(new { success = true, employeeId = contract.EmployeeId });
            }
            ModelState.AddModelError("", message);
        }
        
        // Cần tải lại tên NV nếu validation thất bại
        var employee = await _bManagerService.GetEmployeeByIdAsync(contract.EmployeeId);
        ViewBag.EmployeeNameForModal = employee?.FullName ?? "Employee";

        return PartialView("_CreateContractPartial", contract);
    }
}
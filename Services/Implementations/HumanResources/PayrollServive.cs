using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.DTOs;
using start.Models.ViewModels;
using start.Services;

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;
    private readonly IReportService _reportService;
    private readonly IContractService _contractService;


    public PayrollService(ApplicationDbContext db, IAuthService auth, IWebHostEnvironment env, IReportService reportService, IContractService contractService)

    {
        _db = db; _auth = auth; _env = env; _reportService = reportService; _contractService = contractService;
    }

    // Services/EmployeeProfileService.cs
    public async Task<MonthlySalaryVm?> GetMonthlySalaryAsync(string employeeId, int year, int month)
    {
        var m1 = new DateTime(year, month, 1);
        var slr = await _db.Salaries
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeID == employeeId && s.SalaryMonth == m1);

        if (slr is null) return null;

        var adjs = await _db.SalaryAdjustments
            .Where(a => a.EmployeeID == employeeId && a.AdjustmentDate.Year == year && a.AdjustmentDate.Month == month)
            .OrderBy(a => a.AdjustmentDate)
            .Select(a => new AdjustmentVm(a.AdjustmentDate, a.Amount, a.Reason))
            .ToListAsync();

        // ===== NEW: Kế hoạch =====
        const decimal HOURS_PER_SHIFT = 5.0m; // nếu bạn muốn đọc từ cấu hình/DB thì thay chỗ này
        int scheduledShifts = await _db.WorkSchedules
            .CountAsync(w => w.EmployeeID == employeeId
                          && w.Date.Year == year
                          && w.Date.Month == month);

        decimal plannedHours = scheduledShifts * HOURS_PER_SHIFT;
        decimal potentialBaseSalary = plannedHours * slr.HourlyRateAtTimeOfCalc;

        return new MonthlySalaryVm(
            slr.EmployeeID,
            slr.Employee?.FullName,
            slr.SalaryMonth,
            slr.TotalShifts,
            slr.TotalHoursWorked,
            slr.HourlyRateAtTimeOfCalc,
            slr.BaseSalary,
            slr.Bonus,
            slr.Penalty,
            slr.TotalSalary,
            slr.Status,
            adjs,

            // ==== NEW ====
            scheduledShifts,
            plannedHours,
            HOURS_PER_SHIFT,
            potentialBaseSalary
        );
    }
    public async Task<(bool success, string message)> CalculateAndFinalizeSalaryAsync(string employeeId, int year, int month, int branchId)
    {
        // Giới hạn số lần chốt lại
        const int Max_Retries = 3;
        var m1 = new DateTime(year, month, 1);

        var existingSalary = await _db.Salaries
            .FirstOrDefaultAsync(s => s.EmployeeID == employeeId && s.SalaryMonth == m1);

        //  KIỂM TRA GIỚI HẠN CHỐT LẠI 
        if (existingSalary != null && existingSalary.FinalizationCount >= Max_Retries)
        {
            return (false, $"Lương tháng này đã được chốt lại tối đa {Max_Retries} lần. Không thể chốt lại nữa.");
        }

        // 2. Tính toán lương bằng ReportService 
        var reports = await _reportService.GetSalaryReportAsync(employeeId, month, year, branchId);
        var liveReport = reports.FirstOrDefault();

        if (liveReport == null)
        {
            // Kiểm tra xem lỗi có phải do ReportService không tìm thấy dữ liệu chấm công không
            if (existingSalary == null)
            {
                return (false, "Không tìm thấy dữ liệu chấm công hợp lệ để chốt lương.");
            }
        }

        // Tính HourlyRate trung bình từ Live Report 
        decimal hourlyRate;
        if (liveReport != null && liveReport.TotalHours > 0)
        {
            // Rate trung bình thực tế
            hourlyRate = liveReport.BaseSalary / (decimal)liveReport.TotalHours;
        }
        else
        {
            // Lấy Rate dự phòng từ hợp đồng nếu chưa có giờ làm 
            var contract = await _contractService.GetActiveHourlyContractOnDateAsync(employeeId, m1);
            hourlyRate = contract?.BaseRate ?? 0m;
        }

        // 4. TẠO HOẶC CẬP NHẬT bản ghi trong bảng Salary
        if (existingSalary == null)
        {
            // 4a. TẠO MỚI (Lần chốt 1)
            existingSalary = new Salary
            {
                EmployeeID = employeeId,
                SalaryMonth = m1,
                CalculatedAt = DateTime.Now,
                FinalizationCount = 1
            };
            _db.Salaries.Add(existingSalary);
        }
        else
        {
            // 4b. CẬP NHẬT (Lần chốt 2, 3)
            existingSalary.FinalizationCount += 1;
        }

        // Cập nhật các trường dữ liệu 
        existingSalary.TotalShifts = liveReport?.TotalShifts ?? existingSalary.TotalShifts;
        existingSalary.TotalHoursWorked = (decimal)(liveReport?.TotalHours ?? (double)existingSalary.TotalHoursWorked);
        existingSalary.HourlyRateAtTimeOfCalc = hourlyRate;
        existingSalary.BaseSalary = liveReport?.BaseSalary ?? existingSalary.BaseSalary;
        existingSalary.Bonus = liveReport?.Bonus ?? existingSalary.Bonus;
        existingSalary.Penalty = liveReport?.Penalty ?? existingSalary.Penalty;
        existingSalary.Status = "Đã chốt";
        existingSalary.UpdatedAt = DateTime.Now;

        // 5. Lưu vào Database (có try-catch để bắt lỗi DB)
        try
        {
            await _db.SaveChangesAsync();
            // Trả về thông báo kèm số lần chốt hiện tại
            return (true, $"Đã chốt lương thành công. (Lần chốt: {existingSalary.FinalizationCount})");
        }
        catch (DbUpdateException ex)
        {
            var innerEx = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Lỗi ghi DB: {innerEx}");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi không xác định: {ex.Message}");
        }
    }

    public async Task<ManagerSalaryDetailVm?> GetManagerMonthlySalaryAsync(string managerId, int year, int month)
    {
        var contract = await _contractService.GetActiveHourlyContractOnDateAsync(managerId, new DateTime(year, month, 1));

        if (contract == null || contract.PaymentType != "Tháng")
        {
            return null; // Or handle appropriately if manager doesn't have a monthly salary contract
        }

        decimal grossSalary = contract.BaseRate;

        // --- Constants for calculation ---
        const decimal PersonalDeduction = 11000000m; // Giảm trừ gia cảnh cho bản thân
        const decimal DependentDeduction = 4400000m;  // Giảm trừ cho người phụ thuộc (giả sử là 0)
        const decimal InsuranceRate = 0.105m; // 8% BHXH + 1.5% BHYT + 1% BHTN

        // --- Calculations ---
        // 1. Bảo hiểm
        decimal insurance = grossSalary * InsuranceRate;

        // 2. Thu nhập chịu thuế (TNCT)
        decimal taxableIncome = grossSalary - insurance;

        // 3. Thu nhập tính thuế (TNTT)
        decimal assessableIncome = taxableIncome - PersonalDeduction - (0 * DependentDeduction); // Assuming 0 dependents for now
        if (assessableIncome < 0)
        {
            assessableIncome = 0;
        }

        // 4. Tính thuế TNCN theo biểu thuế lũy tiến
        decimal incomeTax = CalculateProgressiveTax(assessableIncome);

        // 5. Lương thực nhận (NET)
        decimal netSalary = grossSalary - insurance - incomeTax;

        return new ManagerSalaryDetailVm
        {
            GrossSalary = grossSalary,
            Insurance = insurance,
            TaxableIncome = taxableIncome,
            PersonalDeduction = PersonalDeduction,
            AssessableIncome = assessableIncome,
            IncomeTax = incomeTax,
            NetSalary = netSalary
        };
    }

    private decimal CalculateProgressiveTax(decimal assessableIncome)
    {
        if (assessableIncome <= 0) return 0;

        decimal tax = 0;

        // Bậc 1: đến 5 triệu, thuế suất 5%
        if (assessableIncome > 0)
        {
            decimal taxableAtThisLevel = Math.Min(assessableIncome, 5000000m);
            tax += taxableAtThisLevel * 0.05m;
        }
        // Bậc 2: trên 5 triệu đến 10 triệu, thuế suất 10%
        if (assessableIncome > 5000000m)
        {
            decimal taxableAtThisLevel = Math.Min(assessableIncome - 5000000m, 5000000m);
            tax += taxableAtThisLevel * 0.10m;
        }
        // Bậc 3: trên 10 triệu đến 18 triệu, thuế suất 15%
        if (assessableIncome > 10000000m)
        {
            decimal taxableAtThisLevel = Math.Min(assessableIncome - 10000000m, 8000000m);
            tax += taxableAtThisLevel * 0.15m;
        }
        // Bậc 4: trên 18 triệu đến 32 triệu, thuế suất 20%
        if (assessableIncome > 18000000m)
        {
            decimal taxableAtThisLevel = Math.Min(assessableIncome - 18000000m, 14000000m);
            tax += taxableAtThisLevel * 0.20m;
        }
        // Các bậc thuế cao hơn có thể thêm vào đây nếu cần
        // ...

        return tax;
    }


}
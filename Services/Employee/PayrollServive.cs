using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Models.ViewModels;

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IWebHostEnvironment _env;

    public PayrollService(ApplicationDbContext db, IAuthService auth, IWebHostEnvironment env)

    {
        _db = db; _auth = auth; _env = env;
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
                          && w.WorkDate.Year == year
                          && w.WorkDate.Month == month);

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

}
namespace start.Models.ViewModels;

public record AdjustmentVm(DateTime Date, decimal Amount, string? Reason);

public record MonthlySalaryVm(
    string EmployeeID,
    string? EmployeeName,
    DateTime SalaryMonth,
    int TotalShifts,
    decimal TotalHoursWorked,
    decimal HourlyRate,
    decimal BaseSalary,
    decimal Bonus,
    decimal Penalty,
    decimal TotalSalary,
    string Status,
    IReadOnlyList<AdjustmentVm> Adjustments,
    int ScheduledShifts,              // Tổng ca KẾ HOẠCH trong tháng (cả đã làm + dự kiến)
    decimal PlannedHours,             // Giờ kế hoạch = ScheduledShifts * HoursPerShift
    decimal HoursPerShift,            // số giờ / ca (ví dụ 5.0)
    decimal PotentialBaseSalary
);
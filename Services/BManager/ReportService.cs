using start.Data;
using start.Models; // Chứa WorkSchedule và các Models khác
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

// Đảm bảo DetailedShiftReport đã được tham chiếu đúng (qua using hoặc namespace)

namespace start.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IContractService _contractService;
        private const string OrderStatusCompleted = "Đã giao";
        private  const string FinalizedStatus = "Đã chốt";

        public ReportService(ApplicationDbContext context, IContractService contractService)
        {
            _context = context;
            _contractService = contractService;
        }

        // ---------------------------------------------------
        // 1. HELPER METHOD: TÍNH HỆ SỐ LƯƠNG TĂNG CA (OT MULTIPLIER)
        // ---------------------------------------------------
      private double GetHourlyMultiplier(DateTime date)
{
    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
    {
        return 2.0;
    }
    else
    {
        return 1.0; // Mặc định 100% nếu không có bằng chứng OT rõ ràng
    }
}

        // ---------------------------------------------------
        // 2. SALARY REPORT (BÁO CÁO TỔNG HỢP)
        // ---------------------------------------------------
        public async Task<List<SalaryReport>> GetSalaryReportAsync(string? name, int month, int year, int branchId)
{
    // 1. Lấy danh sách nhân viên hợp lệ trong chi nhánh
    var employeesInBranch = await _context.Employees
        .Where(e => e.BranchID == branchId && e.IsActive && (e.RoleID == "EM" || e.RoleID == "SL" || e.RoleID == "SH"))
        .Where(e => string.IsNullOrEmpty(name) || e.FullName.Contains(name))
        .Select(e => new { e.EmployeeID, e.FullName })
        .ToListAsync();

    if (!employeesInBranch.Any())
    {
        return new List<SalaryReport>();
    }

    var employeeIds = employeesInBranch.Select(e => e.EmployeeID).ToList();
    var startDate = new DateTime(year, month, 1);
    var endDate = startDate.AddMonths(1);

    // 2. SỬA LỖI: Lấy tất cả bản ghi chấm công (Attendances) trong tháng của các nhân viên đó
    var attendances = await _context.Attendances
        .Where(a => employeeIds.Contains(a.EmployeeID) &&
                    a.CheckInTime >= startDate &&
                    a.CheckInTime < endDate &&
                    a.CheckOutTime.HasValue)
        .ToListAsync();

    // 3. Lấy tất cả các khoản thưởng/phạt trong tháng
    var adjustments = await _context.SalaryAdjustments
        .Where(a => employeeIds.Contains(a.EmployeeID) &&
                    a.AdjustmentDate >= startDate &&
                    a.AdjustmentDate < endDate)
        .ToListAsync();

    var salaryReports = new List<SalaryReport>();

    foreach (var emp in employeesInBranch)
    {
        var empAttendances = attendances.Where(a => a.EmployeeID == emp.EmployeeID).ToList();
        var empAdjustments = adjustments.Where(a => a.EmployeeID == emp.EmployeeID).ToList();

        double totalHours = 0;
        decimal baseSalary = 0;

        foreach (var attendance in empAttendances)
        {
            var duration = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalHours;
            totalHours += duration;

            // Lấy hợp đồng có hiệu lực tại ngày check-in để tính lương theo giờ
            var contract = await _contractService.GetActiveHourlyContractOnDateAsync(emp.EmployeeID, attendance.CheckInTime.Date);
            if (contract != null && contract.PaymentType == "Giờ")
            {
                baseSalary += (decimal)duration * contract.BaseRate;
            }
        }

        var bonus = empAdjustments.Where(a => a.Amount > 0).Sum(a => a.Amount);
        var penalty = empAdjustments.Where(a => a.Amount < 0).Sum(a => a.Amount);

        salaryReports.Add(new SalaryReport
        {
            EmployeeID = emp.EmployeeID,
            FullName = emp.FullName,
            TotalShifts = empAttendances.Count,
            TotalHours = Math.Round(totalHours, 2),
            BaseSalary = baseSalary,
            Bonus = bonus,
            Penalty = penalty,
            TotalSalary = baseSalary + bonus + penalty
        });
    }

    return salaryReports;
}


        // ---------------------------------------------------
        // 3. DETAILED SHIFT REPORT (BÁO CÁO CA LÀM CHI TIẾT)
        // ---------------------------------------------------
        public async Task<List<DetailedShiftReport>> GetDetailedWorkSchedulesAsync(string employeeId, int month, int year, int branchId)
        {
            // SỬA LỖI: Truy vấn từ bảng Attendances để đảm bảo dữ liệu chính xác và nhất quán
            var attendancesInMonth = await _context.Attendances
                .Include(a => a.WorkSchedule.Employee)
                .Where(a => a.EmployeeID == employeeId &&
                             a.WorkSchedule.Employee.BranchID == branchId &&
                             a.CheckInTime.Month == month &&
                             a.CheckInTime.Year == year &&
                             a.CheckOutTime.HasValue)
                .OrderBy(a => a.CheckInTime)
                .AsNoTracking()
                .ToListAsync();

            var detailedReports = new List<DetailedShiftReport>();
            if (!attendancesInMonth.Any())
            {
                return detailedReports;
            }

            string employeeName = attendancesInMonth.First().WorkSchedule?.Employee?.FullName ?? "N/A";

            foreach (var att in attendancesInMonth)
            {
                double hours = (att.CheckOutTime!.Value - att.CheckInTime).TotalHours;

                var contract = await _contractService.GetActiveHourlyContractOnDateAsync(employeeId, att.CheckInTime.Date);
                decimal hourlyRate = (contract != null && contract.PaymentType == "Giờ") ? contract.BaseRate : 0m;

                double multiplier = GetHourlyMultiplier(att.CheckInTime.Date);
                decimal totalShiftPay = (decimal)hours * hourlyRate * (decimal)multiplier;

                detailedReports.Add(new DetailedShiftReport
                {
                    WorkScheduleID = att.WorkScheduleID ?? 0, EmployeeID = att.EmployeeID, Date = att.CheckInTime.Date,
                    CheckInTime = att.CheckInTime, CheckOutTime = att.CheckOutTime, Shift = att.WorkSchedule?.Shift,
                    FullName = employeeName, 
                    BaseRate = hourlyRate, Multiplier = multiplier, TotalShiftPay = totalShiftPay,
                });
            }

            return detailedReports;
        }

        // ---------------------------------------------------
        // 4. WORK SCHEDULES (Hỗ trợ Export Excel Tổng thể)
        // ---------------------------------------------------
        public async Task<List<WorkSchedule>> GetAllWorkSchedulesForMonthAsync(int month, int year, int branchId)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .Where(ws => ws.IsActive == true && ws.BranchId == branchId &&
                             ws.Date.Month == month && ws.Date.Year == year &&
                             ws.CheckInTime.HasValue && ws.CheckOutTime.HasValue)
                .ToListAsync();
        }
        
        // ---------------------------------------------------
        // 5. REVENUE REPORTS (Các hàm khác giữ nguyên)
        // ---------------------------------------------------
        public async Task<List<RevenueReport>> GetRevenueReportAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var revenueData = await _context.Orders
                .Where(o => o.BranchID == branchId)
                .Where(o => o.Status == OrderStatusCompleted && o.UpdatedAt.HasValue)
                .Where(o => o.UpdatedAt.Value >= startDate.Date && o.UpdatedAt.Value <= inclusiveEndDate)
                .GroupBy(o => o.UpdatedAt.Value.Date)
                .Select(group => new RevenueReport
                {
                    Date = group.Key, TotalOrders = group.Count(), TotalRevenue = group.Sum(o => o.Total)
                })
                .OrderBy(r => r.Date).AsNoTracking().ToListAsync();

            return revenueData;
        }

        public async Task<List<Order>> GetOrdersByDateAsync(int branchId, DateTime date)
        {
            return await _context.Orders
                .Where(o => o.BranchID == branchId && o.Status == OrderStatusCompleted &&
                            o.UpdatedAt.HasValue && o.UpdatedAt.Value.Date == date.Date)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails).ThenInclude(od => od.ProductSize)
                .AsNoTracking().ToListAsync();
        }
    }
}
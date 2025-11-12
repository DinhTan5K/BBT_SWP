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
            // --- KHỞI TẠO VÀ TRUY VẤN DỮ LIỆU ---
            month = (month == 0) ? DateTime.Now.Month : month;
            year = (year == 0) ? DateTime.Now.Year : year;

            var targetMonth = new DateTime(year, month, 1);
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _context.WorkSchedules
                .Include(ws => ws.Employee)
                .Where(ws => ws.IsActive == true && ws.BranchId == branchId &&
                             ws.Date.Month == month && ws.Date.Year == year &&
                             ws.CheckInTime.HasValue && ws.CheckOutTime.HasValue && ws.Employee != null)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(ws => ws.EmployeeID == name || ws.Employee!.FullName.Contains(name));

            var schedules = await query.ToListAsync();

            // Truy vấn điều chỉnh lương và trạng thái chốt lương
            var adjustments = await _context.SalaryAdjustments
                .Where(a => a.AdjustmentDate >= startDate && a.AdjustmentDate <= endDate && a.Employee!.BranchID == branchId)
                .ToListAsync();

            var finalizedEmployeeIds = await _context.Salaries
                .Where(s => s.SalaryMonth == targetMonth && s.Status == FinalizedStatus)
                .Select(s => s.EmployeeID)
                .ToListAsync();

            var groupedSchedules = schedules.GroupBy(ws => ws.EmployeeID).ToList();
            var reports = new List<SalaryReport>();

            foreach (var group in groupedSchedules)
            {
                string employeeId = group.Key!;
                string fullName = group.First().Employee!.FullName;
                double totalHours = 0;
                decimal totalBaseSalary = 0; 
                decimal totalOvertimeSalary = 0; 

                foreach (var ws in group)
                {
                    double hours = (ws.CheckOutTime!.Value - ws.CheckInTime!.Value).TotalHours;
                    totalHours += hours;

                    var contract = await _contractService.GetActiveHourlyContractOnDateAsync(employeeId, ws.Date);
                    decimal hourlyRate = (contract != null && contract.PaymentType == Contract.PaymentTypes.Gio) ? contract.BaseRate : 0m;

                    // Sử dụng hàm helper chính thức
                    double multiplier = GetHourlyMultiplier(ws.Date);

                    decimal standardPayForShift = (decimal)hours * hourlyRate;
                    totalBaseSalary += standardPayForShift;

                    if (multiplier > 1.0)
                    {
                        decimal overtimePremiumRate = (decimal)(multiplier - 1.0);
                        decimal overtimePremiumPay = standardPayForShift * overtimePremiumRate;
                        totalOvertimeSalary += overtimePremiumPay;
                    }
                } 

                var empAdj = adjustments.Where(a => a.EmployeeID == employeeId).ToList();
                decimal bonus = empAdj.Where(a => a.Amount > 0).Sum(a => a.Amount);
                decimal penalty = empAdj.Where(a => a.Amount < 0).Sum(a => Math.Abs(a.Amount));
                decimal finalTotalSalary = totalBaseSalary + totalOvertimeSalary + bonus - penalty;

                reports.Add(new SalaryReport
                {
                    EmployeeID = employeeId, FullName = fullName, TotalShifts = group.Count(), TotalHours = totalHours,
                    BaseSalary = totalBaseSalary, TotalOvertimeSalary = totalOvertimeSalary, 
                    Bonus = bonus, Penalty = penalty, TotalSalary = finalTotalSalary, 
                    IsFinalized = finalizedEmployeeIds.Contains(employeeId)
                });
            }

            return reports.OrderBy(r => r.FullName).ToList();
        }

        // ---------------------------------------------------
        // 3. DETAILED SHIFT REPORT (BÁO CÁO CA LÀM CHI TIẾT)
        // ---------------------------------------------------
        public async Task<List<DetailedShiftReport>> GetDetailedWorkSchedulesAsync(string employeeId, int month, int year, int branchId)
        {
            var shifts = await _context.WorkSchedules
                .Include(ws => ws.Employee) 
                .Where(ws => ws.EmployeeID == employeeId && ws.BranchId == branchId &&
                             ws.Date.Month == month && ws.Date.Year == year &&
                             ws.CheckInTime.HasValue && ws.CheckOutTime.HasValue && ws.IsActive == true)
                .OrderBy(ws => ws.Date)
                .ThenBy(ws => ws.CheckInTime)
                .AsNoTracking()
                .ToListAsync();

            var detailedReports = new List<DetailedShiftReport>();
            string employeeName = shifts.FirstOrDefault()?.Employee?.FullName ?? "N/A";

            foreach (var ws in shifts)
            {
                double hours = (ws.CheckOutTime!.Value - ws.CheckInTime!.Value).TotalHours;

                var contract = await _contractService.GetActiveHourlyContractOnDateAsync(employeeId, ws.Date);
                decimal hourlyRate = (contract != null && contract.PaymentType == Contract.PaymentTypes.Gio) ? contract.BaseRate : 0m;

                double multiplier = GetHourlyMultiplier(ws.Date);
                decimal totalShiftPay = (decimal)hours * hourlyRate * (decimal)multiplier;

                detailedReports.Add(new DetailedShiftReport
                {
                    WorkScheduleID = ws.WorkScheduleID, EmployeeID = ws.EmployeeID!, Date = ws.Date,
                    CheckInTime = ws.CheckInTime, CheckOutTime = ws.CheckOutTime, Shift = ws.Shift,
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
using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] EmployeeAndShiftLeadRoles = { "EM", "SL", "SP" };
        private const string OrderStatusCompleted = "Đã giao";

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BranchManagerDashboardViewModel> GetDashboardSummaryAsync(int branchId)
        {
            // === 1. Lấy thông tin cơ bản ===
            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == branchId);
            if (branch == null)
            {
                return new BranchManagerDashboardViewModel { BranchName = "Không tìm thấy chi nhánh" };
            }

            var today = DateTime.Today;

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                startOfWeek = startOfWeek.AddDays(-7);
            }

            // === 2. Đếm số lượng nhân viên ===
            var employeeCount = await _context.Employees
                .AsNoTracking()
                .CountAsync(e => e.BranchID == branchId && e.IsActive && EmployeeAndShiftLeadRoles.Contains(e.RoleID));

            // === 3. Lấy tất cả đơn hàng ĐÃ GIAO trong tuần ===
            var allDeliveredOrdersInWeek = await _context.Orders
                .AsNoTracking()
                .Where(o => o.BranchID == branchId &&
                            o.Status == OrderStatusCompleted &&
                            o.UpdatedAt.HasValue &&
                            o.UpdatedAt.Value.Date >= startOfWeek &&
                            o.UpdatedAt.Value.Date <= today)
                .ToListAsync();

            // === 4. Tính toán thống kê hôm nay ===
            var todayOrders = allDeliveredOrdersInWeek
                .Where(o => o.UpdatedAt.Value.Date == today)
                .ToList();

            var todayOrdersCount = todayOrders.Count;
            var todayRevenue = todayOrders.Sum(o => o.Total);

            // === 5. Tính toán thống kê tuần ===
            var weekOrdersCount = allDeliveredOrdersInWeek.Count;
            var weekRevenue = allDeliveredOrdersInWeek.Sum(o => o.Total);

            // === 6. Dữ liệu biểu đồ doanh thu 7 ngày ===
            var revenueChart = Enumerable.Range(0, 7)
                .Select(i => startOfWeek.AddDays(i))
                .Select(date => new RevenueChartData
                {
                    Day = GetDayName(date.DayOfWeek),
                    Revenue = allDeliveredOrdersInWeek
                        .Where(o => o.UpdatedAt.Value.Date == date)
                        .Sum(o => (decimal?)o.Total) ?? 0,
                    Orders = allDeliveredOrdersInWeek
                        .Count(o => o.UpdatedAt.Value.Date == date)
                })
                .ToList();

            // === 7. Đơn hàng theo giờ (hôm nay) ===
            var ordersByHour = Enumerable.Range(8, 15)
                .Select(hour => new OrderByHourData
                {
                    Hour = $"{hour}h",
                    Orders = todayOrders.Count(o => o.UpdatedAt.Value.Hour == hour)
                })
                .ToList();

            //=== 8. Sản phẩm bán chạy (hôm nay) ===
            var todayOrderIds = todayOrders.Select(o => o.OrderID).ToList();
            var topProducts = await _context.OrderDetails
                .AsNoTracking()
                .Where(od => todayOrderIds.Contains(od.OrderID))
                .Include(od => od.Product)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new TopProductData
                {
                    Name = g.Key,
                    Sold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Total),
                })
                .OrderByDescending(p => p.Sold)
                .Take(4)
                .ToListAsync();

            var colors = new[] { "#3b82f6", "#10b981", "#f59e0b", "#8b5cf6" };
            for (int i = 0; i < topProducts.Count; i++)
            {
                topProducts[i].Color = colors[i % colors.Length];
            }


            var recentActivities = new List<RecentActivityData>
            {
                new RecentActivityData { Time = DateTime.Now.AddMinutes(-15).ToString("HH:mm"), Action = "Đơn hàng #ODR004 đã hoàn thành", Type = "order" },
                new RecentActivityData { Time = DateTime.Now.AddHours(-1).ToString("HH:mm"), Action = "Tôn Thất Kiệt đã check-in", Type = "checkin" },
                new RecentActivityData { Time = DateTime.Now.AddHours(-2).ToString("HH:mm"), Action = "Nhập kho 20kg trân châu", Type = "inventory" },
            };


            return new BranchManagerDashboardViewModel
            {
                BranchName = branch.Name,
                ManagerName = "Quản lý",
                EmployeeCount = employeeCount,
                TodayOrdersCount = todayOrdersCount,
                TodayRevenue = todayRevenue,
                WeekOrdersCount = weekOrdersCount,
                WeekRevenue = weekRevenue,
                WeekPerformance = 92,
                RevenueChart = revenueChart,
                OrdersByHour = ordersByHour,
                TopProducts = topProducts,
                RecentActivities = recentActivities,
                TargetOrders = branch.TargetOrders,
                TargetRevenue = branch.TargetRevenue
            };
        }
        public async Task<(bool Success, string ErrorMessage)> UpdateBranchTargetsAsync(int branchId, int targetOrders, decimal targetRevenue)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null)
            {
                return (false, "Không tìm thấy chi nhánh.");
            }

            // Gán giá trị mới
            branch.TargetOrders = targetOrders;
            branch.TargetRevenue = targetRevenue;

            await _context.SaveChangesAsync();
            return (true, "Cập nhật mục tiêu thành công.");
        }

        private string GetDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                DayOfWeek.Sunday => "CN",
                _ => ""
            };
        }


        public async Task<List<WeeklySummary>> GetWeeklyEmployeeSummaryAsync(int branchId)
{
    var today = DateTime.Today;
    var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
    if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);

    // SỬA LỖI: Truy vấn từ bảng Attendances để lấy giờ làm thực tế
    var employeeAttendances = await _context.Attendances
        .AsNoTracking()
        .Include(a => a.WorkSchedule.Employee)
        .Where(a => a.WorkSchedule.Employee.BranchID == branchId &&
                    a.CheckInTime >= startOfWeek &&
                    a.CheckOutTime.HasValue)
        .GroupBy(a => new { a.EmployeeID, a.WorkSchedule.Employee.FullName, a.WorkSchedule.Employee.AvatarUrl })
        .Select(g => new WeeklySummary
        {
            EmployeeID = g.Key.EmployeeID,
            FullName = g.Key.FullName,
            // Tính tổng giờ làm từ DateDiffSecond để có độ chính xác cao, sau đó chia cho 3600
            TotalHours = Math.Round(g.Sum(a => EF.Functions.DateDiffSecond(a.CheckInTime, a.CheckOutTime.Value)) / 3600.0, 1),
            TotalShifts = g.Count() // Mỗi bản ghi attendance là một ca đã làm
        })
        .OrderByDescending(s => s.TotalHours)
        .ToListAsync();

    return employeeAttendances;
}
    }
}
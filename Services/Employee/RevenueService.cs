using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;

namespace start.Services
{
    public class RevenueSummary
    {
        public int TotalOrders { get; set; }
        public int Completed { get; set; }
        public int Delivering { get; set; }
        public int Cancelled { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ProductStat
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; } = "";
        public double Revenue { get; set; }
    }

    public class RevenueService
    {
        private readonly ApplicationDbContext _context;

        public RevenueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Order> Orders,
                           List<ProductStat> ProductStats,
                           RevenueSummary Summary)>
            GetRevenueAsync(int branchId, DateTime start, DateTime end)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o =>
                    o.BranchID == branchId &&
                    o.CreatedAt >= start &&
                    o.CreatedAt <= end)
                .ToListAsync();

            var summary = new RevenueSummary
            {
                TotalOrders = orders.Count,
                Completed = orders.Count(o => o.Status == "Đã giao"),
                Delivering = orders.Count(o => o.Status == "Đang giao"),
                Cancelled = orders.Count(o => o.Status == "Đã hủy"),
                TotalRevenue = orders
                    .Where(o => o.Status == "Đã giao")
                    .Sum(o => o.Total)
            };

            var productStats = orders
                .Where(o => o.Status == "Đã giao" && o.OrderDetails != null)
                .SelectMany(o => o.OrderDetails!)
                .Where(od => od.Product != null)
                .GroupBy(od => od.Product!.ProductName)
                .Select(g => new ProductStat
                {
                    ProductName = g.Key?? "Unknown",
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            return (orders, productStats, summary);
        }

        // BIỂU ĐỒ DOANH THU (30 phút)
        public List<ChartPoint> GetRevenueChart(List<Order> orders)
        {
            return orders
                .Where(o => o.Status == "Đã giao")
                .GroupBy(o => new
                {
                    Interval = new DateTime(
                        o.CreatedAt.Year,
                        o.CreatedAt.Month,
                        o.CreatedAt.Day,
                        o.CreatedAt.Hour,
                        o.CreatedAt.Minute < 30 ? 0 : 30,
                        0
                    )
                })
                .Select(g => new ChartPoint
                {
                    Label = g.Key.Interval.ToString("HH:mm"),
                    Revenue = g.Sum(o => (double)o.Total)
                })
                .OrderBy(c => c.Label)
                .ToList();
        }
    }
}

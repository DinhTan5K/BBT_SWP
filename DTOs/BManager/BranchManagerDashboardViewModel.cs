using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace start.Models
{

    public class BranchManagerDashboardViewModel
    {
        public string? ManagerName { get; set; }
        public string? BranchName { get; set; }
        public int EmployeeCount { get; set; }
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int WeekOrdersCount { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal WeekPerformance { get; set; }
        public int TargetOrders { get; set; }
        public decimal TargetRevenue { get; set; }
        public List<RevenueChartData> RevenueChart { get; set; } = new List<RevenueChartData>();
        public List<OrderByHourData> OrdersByHour { get; set; } = new List<OrderByHourData>();
        public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
        public List<RecentActivityData> RecentActivities { get; set; } = new List<RecentActivityData>();
    }

    public class RevenueChartData
    {
        public string Day { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class OrderByHourData
    {
        public string Hour { get; set; } = string.Empty;
        public int Orders { get; set; }
    }

    public class TopProductData
    {
        public string Name { get; set; } = string.Empty;
        public int Sold { get; set; }
        public decimal Revenue { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class RecentActivityData
    {
        public string Time { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
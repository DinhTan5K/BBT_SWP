public class BranchManagerDashboardViewModel
{
    public string ManagerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int TodayOrdersCount { get; set; }
    public decimal TodayRevenue { get; set; }
}
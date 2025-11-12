namespace start.Models.ViewModels
{
    public class MarketingKPIVm
    {
        public string EmployeeID { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public DateTime KpiMonth { get; set; }

        // News Requests Stats
        public int TotalNewsRequests { get; set; }
        public int ApprovedNewsRequests { get; set; }
        public int RejectedNewsRequests { get; set; }
        public int PendingNewsRequests { get; set; }
        public decimal NewsApproveRate { get; set; }

        // Discount Requests Stats
        public int TotalDiscountRequests { get; set; }
        public int ApprovedDiscountRequests { get; set; }
        public int RejectedDiscountRequests { get; set; }
        public int PendingDiscountRequests { get; set; }
        public decimal DiscountApproveRate { get; set; }

        // Overall Stats
        public int TotalRequests { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalPending { get; set; }
        public decimal OverallApproveRate { get; set; }

        // KPI Score
        public decimal KPIScore { get; set; }
        public decimal TargetScore { get; set; }
        public bool IsKPIAchieved { get; set; }

        // Bonus
        public decimal KPIBonus { get; set; }
        public decimal BaseSalary { get; set; }

        public MarketingKPIVm(
            string employeeId,
            string? employeeName,
            DateTime kpiMonth,
            int totalNewsRequests,
            int approvedNewsRequests,
            int rejectedNewsRequests,
            int pendingNewsRequests,
            int totalDiscountRequests,
            int approvedDiscountRequests,
            int rejectedDiscountRequests,
            int pendingDiscountRequests,
            decimal kpiScore,
            decimal targetScore,
            bool isKPIAchieved,
            decimal kpiBonus,
            decimal baseSalary = 0)
        {
            EmployeeID = employeeId;
            EmployeeName = employeeName;
            KpiMonth = kpiMonth;
            TotalNewsRequests = totalNewsRequests;
            ApprovedNewsRequests = approvedNewsRequests;
            RejectedNewsRequests = rejectedNewsRequests;
            PendingNewsRequests = pendingNewsRequests;
            TotalDiscountRequests = totalDiscountRequests;
            ApprovedDiscountRequests = approvedDiscountRequests;
            RejectedDiscountRequests = rejectedDiscountRequests;
            PendingDiscountRequests = pendingDiscountRequests;
            KPIScore = kpiScore;
            TargetScore = targetScore;
            IsKPIAchieved = isKPIAchieved;
            KPIBonus = kpiBonus;
            BaseSalary = baseSalary;

            // Calculate rates
            NewsApproveRate = totalNewsRequests > 0 
                ? (decimal)approvedNewsRequests / totalNewsRequests * 100 
                : 0;
            DiscountApproveRate = totalDiscountRequests > 0 
                ? (decimal)approvedDiscountRequests / totalDiscountRequests * 100 
                : 0;

            TotalRequests = totalNewsRequests + totalDiscountRequests;
            TotalApproved = approvedNewsRequests + approvedDiscountRequests;
            TotalRejected = rejectedNewsRequests + rejectedDiscountRequests;
            TotalPending = pendingNewsRequests + pendingDiscountRequests;

            OverallApproveRate = TotalRequests > 0 
                ? (decimal)TotalApproved / TotalRequests * 100 
                : 0;
        }
    }
}


public class BranchStatisticsDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
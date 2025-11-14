public class RegionStatisticsViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<BranchStatisticsDto> BranchStats { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<RevenueTrendDto> RevenueTrend { get; set; } = new();

     public HourlyHeatmapDto HourlyHeatmap { get; set; }

     public List<Branch> Branches { get; set; } = new();

}



public class RegionDashboardViewModel
{
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public List<Branch> Branches { get; set; } = new List<Branch>();

}

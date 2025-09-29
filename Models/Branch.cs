public class Branch
{
    public int BranchID { get; set; }  // đổi từ Id cho chuẩn
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public ICollection<Order>? Orders { get; set; }
}
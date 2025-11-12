namespace start.Models.ViewModels;

public class DayOffManagerVm
{
    public int Id { get; set; }
    public string EmployeeID { get; set; } = null!;
    public string FullName { get; set; } = null!; 
    public DateTime OffDate { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
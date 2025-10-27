using System.ComponentModel.DataAnnotations;

namespace start.Models.ViewModels;

public class DayOffOneDayVm
{
    [Required] public string EmployeeID { get; set; } = null!;
    public int? BranchID { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Ngày nghỉ")]
    public DateTime OffDate { get; set; } = DateTime.Today.AddDays(3); // gợi ý hợp lệ

    [Required, StringLength(500), Display(Name = "Lý do")]
    public string Reason { get; set; } = "";
}

public class DayOffListItemVm
{
    public int Id { get; set; }
    public DateTime OffDate { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
}
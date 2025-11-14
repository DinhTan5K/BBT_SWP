using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models;

[Table("DayOffRequest")]
public class DayOffRequest
{
    [Key] public int Id { get; set; }

    [Required, StringLength(10)]
    public string EmployeeID { get; set; } = null!;

    public int? BranchID { get; set; }

    [Column(TypeName = "date")]
    public DateTime OffDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected

    [StringLength(10)]
    public string? ApproverID { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(EmployeeID))]
    public Employee Employee { get; set; } = null!;
}
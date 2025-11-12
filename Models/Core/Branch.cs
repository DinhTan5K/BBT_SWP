using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using start.Models;

[Table("Branch")]

public class Branch
{
    [Key]
    [Column("BranchID")]  
    public int BranchID { get; set; }

    [Required]
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    public int RegionID { get; set; }

    [StringLength(100)]
    public string? City { get; set; }
    
    [Precision(18, 15)]
    public decimal Latitude { get; set; }

    [Precision(18, 15)]
    public decimal Longitude { get; set; }
    
    // Navigation properties
    public ICollection<Order>? Orders { get; set; }
    
    [ForeignKey(nameof(RegionID))]
    public Region? Region { get; set; }
}

 
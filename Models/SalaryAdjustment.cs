using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace start.Models
{
    [Table("SalaryAdjustments")]
    public class SalaryAdjustment
    {
        [Key]
        public int AdjustmentID { get; set; }

        [Required, StringLength(10)]
        public string EmployeeID { get; set; } = null!;   // <-- thêm null!

        [Column(TypeName = "date")]
        public DateTime AdjustmentDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // >0 thưởng, <0 phạt

        [StringLength(255)]
        public string? Reason { get; set; }               // <-- cho phép null

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }           // <-- cho phép null
    }
}
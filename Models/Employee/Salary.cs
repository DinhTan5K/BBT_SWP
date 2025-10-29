using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace start.Models
{
    [Table("Salary")]
    [Index(nameof(EmployeeID), nameof(SalaryMonth), IsUnique = true, Name = "UQ_Employee_SalaryMonth")]
    public class Salary
    {
        [Key]
        public int SalaryID { get; set; }

        [Required, StringLength(10)]
        public string EmployeeID { get; set; } = null!;   // <-- thêm null!

        [Column(TypeName = "date")]
        public DateTime SalaryMonth { get; set; } // luôn là ngày 01

        public int TotalShifts { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalHoursWorked { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRateAtTimeOfCalc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Penalty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal TotalSalary { get; private set; }

        [StringLength(255)]
        public string? Notes { get; set; }                // <-- cho phép null

        [StringLength(50)]
        public string Status { get; set; } = "Chưa thanh toán";

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }           // <-- cho phép null
    }
}
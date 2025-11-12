using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

namespace start.Models
{
    [Table("Attendance")]
    public class Attendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttendanceID { get; set; }

        [ForeignKey("Employee")]
        [Required]
        [StringLength(10)]
        [Column("EmployeeID", TypeName = "nvarchar(10)")]
        public string EmployeeID { get; set; } = null!;
        public Employee? Employee { get; set; }

        [ForeignKey("WorkSchedule")]
        public int? WorkScheduleID { get; set; }
        public WorkSchedule? WorkSchedule { get; set; }

        [Required]
        [Display(Name = "Check-In Time")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
        public DateTime CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}")]
        public DateTime? CheckOutTime { get; set; }

        [StringLength(500)]
        public string? CheckInImageUrl { get; set; }

        [StringLength(500)]
        public string? CheckOutImageUrl { get; set; }

      

        public bool IsFaceVerified { get; set; } = false;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}


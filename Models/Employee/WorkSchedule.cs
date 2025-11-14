using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

namespace start.Models
{
    [Table("WorkSchedule")]
    public class WorkSchedule
    {
        [Key] 
        public int WorkScheduleID { get; set; }

        [ForeignKey("Employee")]
        public string? EmployeeID { get; set; }

        public Employee? Employee { get; set; }

        [Display(Name = "Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }
        public string? Shift { get; set; }

        public bool IsActive { get; set; } = true;
        [Display(Name = "Check-In")]
        public DateTime? CheckInTime { get; set; }

        [Display(Name = "Check-Out")]
        public DateTime? CheckOutTime { get; set; }
        [Required]
        [StringLength(20)]
        public string? Status { get; set; }
        
        [ForeignKey("Branch")]
        public int BranchId { get; set; }
        public Branch? Branch { get; set; }
    }
}

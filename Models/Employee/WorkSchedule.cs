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

        [Display(Name = "Work Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime WorkDate { get; set; }

        public string? Shift { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

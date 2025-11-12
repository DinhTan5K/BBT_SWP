using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using start.Models;

namespace start.Models
{
    [Table("Employee")]
    [Index(nameof(PhoneNumber), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class Employee
    {
        [Key]
        [Column("EmployeeID")]
        [StringLength(10)]
        public string? EmployeeID { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Column("FullName")]
        [StringLength(100)]
        public string? FullName { get; set; }



        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("PhoneNumber")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Column("Email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("City")]
        public string? City { get; set; }


        [Required]
        [Column("RoleID")]
        [StringLength(2)]
        public string? RoleID { get; set; }

        [Column("BranchID")]
        public int? BranchID { get; set; }

        [Column("RegionID")]
        public int? RegionID { get; set; }

        [Required]
        [Column("Password")]
        [StringLength(200)]
       
        public string? Password { get; set; }

        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }


        [Column("Gender")]
        [StringLength(10)]
        public string? Gender { get; set; }

        [Column("Nationality")]
        [StringLength(60)]
        public string? Nationality { get; set; }

        [Column("Ethnicity")]
        [StringLength(60)]
        public string? Ethnicity { get; set; }

        [Column("EmergencyPhone1")]
        [StringLength(20)]
        public string? EmergencyPhone1 { get; set; }

        [Column("EmergencyPhone2")]
        [StringLength(20)]
        public string? EmergencyPhone2 { get; set; }

        [Column("AvatarUrl")]
        [StringLength(300)]
        public string? AvatarUrl { get; set; }
        public bool IsHashed { get; set; } = false;


        [ForeignKey(nameof(RoleID))]
        public Role Role { get; set; } = null!;

        [ForeignKey(nameof(BranchID))]
        public Branch? Branch { get; set; }

        [ForeignKey(nameof(RegionID))]
        public Region? Region { get; set; }
        
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        [InverseProperty(nameof(Salary.Employee))]
        public ICollection<Salary> Salaries { get; set; } = new List<Salary>();

        [InverseProperty(nameof(SalaryAdjustment.Employee))]
        public ICollection<SalaryAdjustment> SalaryAdjustments { get; set; } = new List<SalaryAdjustment>();
    }
}
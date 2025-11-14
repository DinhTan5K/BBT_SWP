using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    

    [Table("EmployeeBranchRequest")]
    public class EmployeeBranchRequest
    {
        [Key]
        public int Id { get; set; }

        public RequestType RequestType { get; set; } = RequestType.Add;

        [StringLength(10)]
        public string? EmployeeId { get; set; }

        public int? BranchId { get; set; }

        public int? RegionID { get; set; }

        // Thông tin nhân viên (dùng khi Add)
        [StringLength(100)]
        public string? FullName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(60)]
        public string? Nationality { get; set; }

        [StringLength(60)]
        public string? Ethnicity { get; set; }

        [StringLength(20)]
        public string? EmergencyPhone1 { get; set; }

        [StringLength(20)]
        public string? EmergencyPhone2 { get; set; }

        [StringLength(2)]
        public string? RoleID { get; set; }

        public bool IsActive { get; set; } = true;

        [Required, StringLength(10)]
        public string RequestedBy { get; set; } = null!;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [StringLength(10)]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }
    }
}

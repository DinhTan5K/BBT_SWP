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

        [Required]
        public RequestType RequestType { get; set; }// Add, Edit, Delete

        // ID của Employee nếu là Edit hoặc Delete (null nếu là Add)
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? EmployeeId { get; set; }

        // ID của Branch cần thêm/chuyển nhân viên vào
        public int? BranchId { get; set; }

        public int? RegionID { get; set; }

        // Thông tin nhân viên (chỉ cần khi RequestType = Add, để tạo nhân viên mới)
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
        public string? RoleID { get; set; } // Mặc định là "EM" khi tạo mới

        // Thông tin người yêu cầu (RM hoặc BM)
        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Thông tin duyệt
        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? ReviewedBy { get; set; } // Admin ID

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; } // Lý do từ chối

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        // Không có Foreign Key constraint vì khi RequestType = Add, nhân viên có thể chưa tồn tại
        // [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}


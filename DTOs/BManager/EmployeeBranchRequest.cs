using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    // Giả sử các Enums RequestType và RequestStatus nằm cùng namespace start.Models
    
    [Table("EmployeeBranchRequest")]
    public class EmployeeBranchRequest
    {
        [Key] 
        public int Id { get; set; }

        // ⭐️ Sử dụng Enum RequestType
        [Required] 
        public RequestType RequestType { get; set; } = RequestType.Add;

        // Thông tin chung
        [StringLength(10)] 
        public string? EmployeeId { get; set; } // ID của Employee (nếu là Edit/Delete)
        public int? BranchId { get; set; } // Branch cần thêm/chuyển đến

        // Thông tin nhân viên (chỉ cần khi RequestType = Add)
        [StringLength(100)] 
        public string? FullName { get; set; }
        
        [Column(TypeName = "date")] 
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(10)] 
        public string? Gender { get; set; }
        
        [StringLength(20)] 
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [NotMapped] 
public string? Password { get; set; }
        
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
        public string? RoleID { get; set; } = "EM"; // Mặc định là Nhân viên

        // Thông tin người yêu cầu
        [Required, StringLength(10)] 
        public string RequestedBy { get; set; } = null!;
        
        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        // ⭐️ Sử dụng Enum RequestStatus
        [Required] 
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        
        [StringLength(10)] 
        public string? ReviewedBy { get; set; }
        
        public DateTime? ReviewedAt { get; set; }
        
        [StringLength(500)] 
        public string? RejectionReason { get; set; }
        
        // Navigation Properties (Khóa ngoại)
        [ForeignKey(nameof(RequestedBy))] 
        public Employee? Requester { get; set; }
        
        [ForeignKey(nameof(ReviewedBy))] 
        public Employee? Reviewer { get; set; }
        
        [ForeignKey(nameof(BranchId))] 
        public Branch? Branch { get; set; }
    }
}
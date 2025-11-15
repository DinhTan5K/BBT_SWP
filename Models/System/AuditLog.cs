using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("AuditLog")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string AdminId { get; set; } = string.Empty; // EmployeeID của Admin thực hiện

        [StringLength(100)]
        public string? AdminName { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE_RM, UPDATE_RM, DEACTIVATE_RM, ACTIVATE_RM

        [StringLength(10)]
        public string? TargetEmployeeId { get; set; } // EmployeeID của RM được thao tác

        [StringLength(100)]
        public string? TargetEmployeeName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; } // Mô tả chi tiết hành động

        [StringLength(50)]
        public string? EntityType { get; set; } = "RM"; // Loại entity (RM, Employee, etc.)

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("DiscountRequest")]
    public class DiscountRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; } // Add, Edit, Delete

        // ID của Discount nếu là Edit hoặc Delete (null nếu là Add)
        public int? DiscountId { get; set; }

        // Thông tin người yêu cầu (RM)
        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Thông tin duyệt
        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? ReviewedBy { get; set; } // Admin ID

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; } // Lý do từ chối

        // Dữ liệu Discount (tất cả các trường của Discount)
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Percent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UsageLimit { get; set; }
        
        public DiscountType Type { get; set; }

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        [ForeignKey(nameof(DiscountId))]
        public Discount? Discount { get; set; }
    }
}


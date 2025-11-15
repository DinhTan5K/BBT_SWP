using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

namespace start.Models
{
    [Table("CategoryRequest")]
    public class CategoryRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; } // Add, Edit, Delete

        // ID của Category nếu là Edit hoặc Delete (null nếu là Add)
        public int? CategoryId { get; set; }

        // Thông tin người yêu cầu (RM)
        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        // Thông tin duyệt
        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? ReviewedBy { get; set; } // Admin ID

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; } // Lý do từ chối

        // Dữ liệu Category
        [Required]
        [StringLength(200)]
        public string CategoryName { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public ProductCategory? Category { get; set; }
    }
}



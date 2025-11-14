using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("ProductRequest")]
    public class ProductRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; } // Add, Edit, Delete

        // ID của Product nếu là Edit hoặc Delete (null nếu là Add)
        public int? ProductId { get; set; }

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

        // Dữ liệu Product (snapshot)
        public string? ProductName { get; set; }

        [Required(ErrorMessage = "Bạn phải chọn Category")]
        public int CategoryID { get; set; }

        public ProductCategory? Category { get; set; }
        public string? Description { get; set; }
        public string? Image_Url { get; set; }

        public bool IsActive { get; set; } = true;

        // Lưu sizes dưới dạng JSON (trong DB có cột ProductSizesJson)
        [Column("ProductSizesJson", TypeName = "nvarchar(max)")]
        public string? ProductSizesJson { get; set; }

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}

using System;
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

        // Dữ liệu Product (tất cả các trường của Product)
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        public string? Description { get; set; }

        public string? Image_Url { get; set; }

        public bool IsActive { get; set; } = true;

        // Lưu ProductSizes dưới dạng JSON string
        // Format: [{"Size":"S","Price":25000},{"Size":"M","Price":30000}]
        [Column(TypeName = "nvarchar(max)")]
        public string? ProductSizesJson { get; set; }

        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [ForeignKey(nameof(CategoryID))]
        public ProductCategory? Category { get; set; }
    }
}



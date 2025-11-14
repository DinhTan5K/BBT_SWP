using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using start.Models;

namespace start.Models
{
    [Table("BranchRequest")]
    public class BranchRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; } // Add, Edit, Delete

        // ID của Branch nếu là Edit hoặc Delete (null nếu là Add)
        public int? BranchId { get; set; }

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

        // Dữ liệu Branch
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public int RegionID { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Precision(18, 15)]
        public decimal? Latitude { get; set; }

        [Precision(18, 15)]
        public decimal? Longitude { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; } // Ghi chú/Lý do
        public bool IsActive { get; set; } = true;


        // Navigation properties
        [ForeignKey(nameof(RequestedBy))]
        public Employee? RequestedByEmployee { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public Employee? ReviewedByEmployee { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        [ForeignKey(nameof(RegionID))]
        public Region? Region { get; set; }

    }
}



using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{


    [Table("CategoryRequest")]
    public class CategoryRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [Column(TypeName = "varchar(10)")]
        [StringLength(10)]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Column(TypeName = "varchar(10)")]
        [StringLength(10)]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [Required, StringLength(200)]
        public string CategoryName { get; set; } = string.Empty;
    }
}

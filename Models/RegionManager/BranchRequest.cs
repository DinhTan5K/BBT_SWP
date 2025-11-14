using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
   

    [Table("BranchRequest")]
    public class BranchRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public RequestType RequestType { get; set; } = RequestType.Add;

        // nếu Edit/Delete -> tham chiếu branch
        public int? BranchId { get; set; }

        [Required, StringLength(10), Column(TypeName = "varchar(10)")]
        public string RequestedBy { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [StringLength(10), Column(TypeName = "varchar(10)")]
        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // Branch data snapshot
        [Required, StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public int RegionID { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Column(TypeName = "decimal(18,15)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,15)")]
        public decimal? Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}

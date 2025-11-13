using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

namespace start.Models.System
{
    [Table("AdminSecurity")]
    public class AdminSecurity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string EmployeeID { get; set; } = string.Empty;

        [Required]
        public bool IsTwoFactorEnabled { get; set; } = false;

        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? TwoFactorType { get; set; }

        [StringLength(256)]
        public string? TwoFactorSecret { get; set; }

        public string? RecoveryCodes { get; set; }

        [StringLength(64)]
        public string? LastOtpCode { get; set; }

        public DateTime? LastOtpExpiredAt { get; set; }

        public int FailedCount { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(EmployeeID))]
        public Employee Employee { get; set; } = null!;
    }
}


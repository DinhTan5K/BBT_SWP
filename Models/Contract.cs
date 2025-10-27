using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("Contract", Schema = "dbo")]
    public class Contract
    {
        [Key]
        [Column("contractid")]
        public int ContractId { get; set; }

        [Required, StringLength(10)]
        [Column("employeeid", TypeName = "varchar(10)")]
        public string EmployeeId { get; set; } = null!;

        [Required, StringLength(50)]
        [Column("contractnumber")]
        public string ContractNumber { get; set; } = null!;

        // 'Thử việc' | '1 năm' | 'Vô thời hạn'
        [Required, StringLength(30)]
        [Column("contracttype", TypeName = "nvarchar(30)")]
        public string ContractType { get; set; } = null!;

        [Required]
        [Column("startdate", TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column("enddate", TypeName = "date")]
        public DateTime? EndDate { get; set; }

        // 'Giờ' | 'Tháng'  (phù hợp CHECK CONSTRAINT)
        [Required, StringLength(10)]
        [Column("paymenttype", TypeName = "nvarchar(10)")]
        public string PaymentType { get; set; } = null!;

        [Required]
        [Column("baserate", TypeName = "decimal(18,2)")]
        public decimal BaseRate { get; set; }

        // 'Hiệu lực' | 'Hết hạn'
        [Required, StringLength(20)]
        [Column("status", TypeName = "nvarchar(20)")]
        public string Status { get; set; } = null!;

        [Required]
        [Column("CreatedAt", TypeName = "datetime2")]
        public DateTime Created_At { get; set; } = DateTime.Now;

         [Column("created_at")]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

         [Column("updated_at")]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        // Helpers (không lưu DB)
        [NotMapped] public bool IsHourly => string.Equals(PaymentType, "Giờ", StringComparison.OrdinalIgnoreCase);
        [NotMapped] public bool IsActive => string.Equals(Status, "Hiệu lực", StringComparison.OrdinalIgnoreCase);

        public static class PaymentTypes { public const string Gio = "Giờ"; public const string Thang = "Tháng"; }
        public static class Statuses { public const string HieuLuc = "Hiệu lực"; public const string HetHan = "Hết hạn"; }
    }
}
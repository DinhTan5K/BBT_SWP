using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    public enum DiscountType
    {
        Percentage = 0,
        FixedAmount = 1,
        FreeShipping = 2,
        FixedShippingDiscount = 3,
        PercentShippingDiscount = 4
    }

    [Table("Discount")]
    public class Discount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // Percentage 0-100
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percent { get; set; }

        // Optional fixed amount in VND
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UsageLimit { get; set; }
        public DiscountType Type { get; set; }
    }
}



using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

        public enum DiscountType
    {
        [Display(Name = "Percentage")]
        Percentage = 0,
        [Display(Name = "Fixed Amount")]
        FixedAmount = 1,
        [Display(Name = "Free Shipping")]
        FreeShipping = 2,
        [Display(Name = "Fixed Shipping Discount")]
        FixedShippingDiscount = 3,
        [Display(Name = "Percent Shipping Discount")]
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



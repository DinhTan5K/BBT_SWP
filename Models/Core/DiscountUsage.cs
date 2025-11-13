using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("DiscountUsage")]
    public class DiscountUsage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscountId { get; set; }

        [Required]
        [MaxLength(10)]
        public string UserId { get; set; } = string.Empty;

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [ForeignKey("DiscountId")]
        public virtual Discount Discount { get; set; }
    }
}

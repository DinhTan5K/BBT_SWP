using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

public class News
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Mã giảm giá đi kèm với tin tức
    public int? DiscountId { get; set; }

    [ForeignKey(nameof(DiscountId))]
    public Discount? Discount { get; set; }
}
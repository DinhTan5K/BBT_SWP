using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ProductReview")]
public class ProductReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReviewID { get; set; }
    
    [Required]
    public int ProductID { get; set; }
    
    [ForeignKey("ProductID")]
    public Product? Product { get; set; }
    
    [Required]
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5 sao")]
    public int Rating { get; set; }
    
    [Required]
    [StringLength(500, ErrorMessage = "Bình luận không quá 500 ký tự")]
    public string Comment { get; set; } = string.Empty;
    
    [Required]
    public string CustomerName { get; set; } = string.Empty;
    
    public int? CustomerID { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

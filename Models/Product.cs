using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace start.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }
        [Required(ErrorMessage = "Product name is required")]
        public string? ProductName { get; set; }
        [Required(ErrorMessage = "Bạn phải chọn Category")]
        public int CategoryID { get; set; }
        public ProductCategory? Category { get; set; }
        public string? Description { get; set; }
        public string? Image_Url { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
    }
}
	
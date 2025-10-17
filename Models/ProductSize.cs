using System.ComponentModel.DataAnnotations.Schema;

    [Table("ProductSize")]
    public class ProductSize
    {
        public int ProductSizeID { get; set; }
        public int ProductID { get; set; }

        public string Size { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public Product? Product { get; set; }
    }

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


    [Table("Product")]
    public class Product
    {
        public int ProductID { get; set; }
        public string? ProductName { get; set; }
        public int CategoryID { get; set; }
        public string? Description { get; set; }
        public string? Image_Url { get; set; }

        public ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
    }


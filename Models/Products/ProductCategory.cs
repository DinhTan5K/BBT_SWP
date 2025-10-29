using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

    [Table("ProductCategory")]
    public class ProductCategory
    {
        [Key]
        public int CategoryID { get; set; }

        public string? CategoryName { get; set; }


        public ICollection<Product>? Products { get; set; }
    }


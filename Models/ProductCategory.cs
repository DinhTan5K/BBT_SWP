using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

[Table("ProductCategory")]
    public class Category
    {
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public ICollection<Product>? Products { get; set; }
    }

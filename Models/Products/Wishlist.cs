using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using start.Models;
using Microsoft.EntityFrameworkCore;

[Table("Wishlist")]
[PrimaryKey(nameof(CustomerID), nameof(ProductID))]  // ✅ Composite key chuẩn EF8
public class Wishlist
{
    public int CustomerID { get; set; }
    [ForeignKey(nameof(CustomerID))]
    public Customer? Customer { get; set; }

    public int ProductID { get; set; }
    [ForeignKey(nameof(ProductID))]
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}






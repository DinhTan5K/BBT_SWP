using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

[Table("CartDetail")] 
public class CartDetail
{
    public int CartDetailID { get; set; }
    public int CartID { get; set; }
    public Cart? Cart { get; set; }
    public int ProductID { get; set; }
    public Product? Product { get; set; }
    public int ProductSizeID { get; set; }
    public ProductSize? ProductSize { get; set; }
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }  

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; } 
}
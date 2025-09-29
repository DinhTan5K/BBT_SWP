using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

[Table("OrderDetail")]
public class OrderDetail
{
    public int OrderDetailID { get; set; }
    public int OrderID { get; set; }
    public Order? Order { get; set; }
    public int ProductID { get; set; }
    public Product? Product { get; set; }
    public int ProductSizeID { get; set; }
    public ProductSize? ProductSize { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
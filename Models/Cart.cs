using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using start.Models;

[Table("Cart")] 
public class Cart
{
    public int CartID { get; set; }
    public int CustomerID { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
}

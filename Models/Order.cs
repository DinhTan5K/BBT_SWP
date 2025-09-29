using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

[Table("Order")]
public class Order
{
    public int OrderID { get; set; }
    public int CustomerID { get; set; }
    public Customer? Customer { get; set; }

    public int BranchID { get; set; }
    public Branch? Branch { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<OrderDetail>? OrderDetails { get; set; }
}

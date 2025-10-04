using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using start.Models;

namespace start.Models
{
    [Table("Order")]
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public Customer? Customer { get; set; }

        [ForeignKey("Branch")]
        public int BranchID { get; set; }
        public Branch? Branch { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? OrderCode { get; set; }

        public string? Status { get; set; } = "Chờ xác nhận";
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        public ICollection<OrderDetail>? OrderDetails
        { get; set; }

    }
}
public class OrderSummaryViewModel
{
    public int OrderID { get; set; }
    public string?OrderCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string?Status { get; set; } // "Hoàn thành", "Đã hủy", "Chờ xác nhận"

    // Thông tin giao hàng
    public string?ReceiverName { get; set; }
    public string?ReceiverPhone { get; set; }
    public string?Address { get; set; }
    public string?DetailAddress { get; set; }
    public string?NoteOrder { get; set; }

    // Danh sách các sản phẩm trong đơn hàng
    public List<OrderDetailItemViewModel>?OrderDetails { get; set; }
}

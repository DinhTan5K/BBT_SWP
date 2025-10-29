public class OrderDetailItemViewModel
{
    public string?ProductName { get; set; }
    public string?ProductImageUrl { get; set; } // Lấy từ Product.Image
    public string?SizeName { get; set; } // Lấy từ ProductSize.Name
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
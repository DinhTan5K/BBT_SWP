using start.Models;

public class CartViewModel
{
    public Cart? Cart { get; set; }
    public List<Discount>? AvailableDiscounts { get; set; } // PHẢI CÓ THUỘC TÍNH NÀY
    // ...
}
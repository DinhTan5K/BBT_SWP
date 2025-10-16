using start.Models;
public interface ICartService
{
    List<object> GetCartItems(int customerId);
    bool AddToCart(int customerId, AddToCartRequest request, out string message);
    bool UpdateCart(int customerId, UpdateCartRequest request, out string message);
  
}

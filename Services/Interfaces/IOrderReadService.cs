using System.Threading.Tasks;
using start.Models;

public interface IOrderReadService
{
    Task<Cart> GetCartForCheckoutAsync(int customerId);
    Task<OrderHistoryViewModel> GetOrderHistoryAsync(int customerId, int page = 1, int pageSize = 10);
}



using start.Models;
using System.Threading.Tasks;


public interface IOrderService
{
    Task<(bool success, string message, int? orderId)> CreateOrderAsync(OrderFormModel form);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<object?> GetOrderByCodeAsync(string orderCode);
}

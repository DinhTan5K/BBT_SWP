using start.Models;

namespace start.Services
{
    public interface IShipperService
    {
        Task<List<Order>> GetMyOrdersAsync(string shipperId);
        Task<string> UpdateOrderStatusAsync(int id, string status, string empId);
    }
}

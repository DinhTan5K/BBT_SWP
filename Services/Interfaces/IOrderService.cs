using start.Models;
using System.Threading.Tasks;


public interface IOrderService
{
    Task<(bool success, string message, int? orderId)> CreateOrderAsync(int customerId, OrderFormModel form);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<object?> GetOrderByCodeAsync(string orderCode);
    Task<PromoCodeResponse> CalculateDiscountAsync(string promoCodes, decimal itemsTotal, decimal shippingFee, int branchID);
     Task<PromoValidationResult> ValidateAndApplyPromoCodesAsync(PromoValidationRequest request);
    Task<(bool success, string message)> CancelByCustomerAsync(int orderId, int customerId, string? reason);
  bool Reorder(int customerId, int orderId, out string message);
  Task UpdateTransIdAsync(int orderId, string transId);
    Task MarkOrderAsDelivered(int orderId);
}

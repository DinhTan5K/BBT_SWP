using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface ICheckoutService
{
    Task<(bool requireMomo, int? orderId, string? error)> CreateOrderOrStartMomoAsync(int customerId, OrderFormModel form, ISession session);
    Task<string> InitiateMomoPaymentAsync(int customerId, ISession session, HttpContext httpContext);
    Task<(bool success, int? orderId)> HandleMomoCallbackAsync(IQueryCollection query, int customerId, ISession session);
}



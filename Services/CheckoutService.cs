using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CheckoutService : ICheckoutService
{
    private readonly IOrderService _orderService;
    private readonly IOrderReadService _orderReadService;
    private readonly IPaymentService _paymentService;

    public CheckoutService(IOrderService orderService, IOrderReadService orderReadService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _orderReadService = orderReadService;
        _paymentService = paymentService;
    }

    public async Task<(bool requireMomo, int? orderId, string? error)> CreateOrderOrStartMomoAsync(int customerId, OrderFormModel form, ISession session)
    {
        if (!string.IsNullOrWhiteSpace(form.Payment) && form.Payment.Trim().Equals("Momo", System.StringComparison.OrdinalIgnoreCase))
        {
            session.SetString("PendingOrderForm", JsonSerializer.Serialize(form));
            return (requireMomo: true, orderId: null, error: null);
        }

        var result = await _orderService.CreateOrderAsync(customerId, form);
        if (!result.success) return (false, null, result.message);
        return (false, result.orderId, null);
    }

    public async Task<string> InitiateMomoPaymentAsync(int customerId, ISession session, HttpContext httpContext)
    {
        var formJson = session.GetString("PendingOrderForm");
        if (string.IsNullOrEmpty(formJson)) throw new System.InvalidOperationException("Không có đơn hàng chờ thanh toán.");
        var form = JsonSerializer.Deserialize<OrderFormModel>(formJson);
        if (form == null) throw new System.InvalidOperationException("Dữ liệu đơn hàng không hợp lệ.");

        var cart = await _orderReadService.GetCartForCheckoutAsync(customerId);
        if (cart == null || cart.CartDetails == null || cart.CartDetails.Count == 0)
            throw new System.InvalidOperationException("Giỏ hàng trống.");

        var itemsTotal = cart.CartDetails.Sum(cd => cd.Total);
        var calc = await _orderService.CalculateDiscountAsync(form.PromoCode, itemsTotal, form.ShippingFee);
        var finalTotal = calc.FinalTotal;

        var orderInfo = $"Thanh toán đơn hàng của KH {customerId}";
        var payUrl = await _paymentService.CreatePaymentAsync(finalTotal, orderInfo, httpContext);
        return payUrl;
    }

    public async Task<(bool success, int? orderId)> HandleMomoCallbackAsync(IQueryCollection query, int customerId, ISession session)
    {
        var isSuccess = await _paymentService.HandleCallbackAsync(query);
        if (!isSuccess)
        {
            session.Remove("PendingOrderForm");
            return (false, null);
        }

        var formJson = session.GetString("PendingOrderForm");
        if (string.IsNullOrEmpty(formJson)) return (false, null);
        var form = JsonSerializer.Deserialize<OrderFormModel>(formJson);
        if (form == null) return (false, null);

        var result = await _orderService.CreateOrderAsync(customerId, form);
        session.Remove("PendingOrderForm");

        if (!result.success) return (false, null);
        return (true, result.orderId);
    }
}



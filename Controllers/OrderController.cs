using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using start.Models;
using start.Data;
using start.Services.Interfaces;
using System.Security.Claims;
using start.DTOs;

[Route("Order")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderReadService _orderReadService;
    private readonly ICheckoutService _checkoutService;
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IDiscountService _discountService;


    public OrderController(IOrderService orderService, IOrderReadService orderReadService, ICheckoutService checkoutService, ApplicationDbContext context, IPaymentService paymentService, IDiscountService discountService)
    {
        _orderService = orderService;
        _orderReadService = orderReadService;
        _checkoutService = checkoutService;
        _context = context;
        _paymentService = paymentService;
        _discountService = discountService;
    }

    // Helper method để lấy CustomerID từ Claims (CustomerScheme)
    private int? GetCustomerId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out int customerId))
            return customerId;
        return null;
    }

    [HttpGet("Track")]
    public IActionResult Track() => View();

    [HttpGet("TrackByCode/{orderCode}")]
    public async Task<IActionResult> TrackByCode(string orderCode)
    {
        var order = await _orderService.GetOrderByCodeAsync(orderCode);
        if (order == null)
            return Json(new { success = false, message = "Order không tồn tại" });

        return Json(new { success = true, order });
    }

    [HttpGet("Confirmed/{id}")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> OrderConfirmed(int id)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
        {
            return RedirectToAction("Login", "Account");
        }
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound();

        if (order.CustomerID != customerId.Value)
            return NotFound();

        return View(order);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> Order()
    {
        // Lấy ID khách hàng từ Claims
        int? customerId = GetCustomerId();

        if (customerId == null)
        {
            // Nếu chưa login, cho về trang đăng nhập
            return RedirectToAction("Login", "Account");
        }
        var customer = await _context.Customers.FindAsync(customerId.Value);

        ViewData["CustomerName"] = customer?.Name;
        ViewData["CustomerPhone"] = customer?.Phone;
        ViewData["CustomerAddress"] = customer?.Address;

        // 🔹 Lấy danh sách chi nhánh từ DB (đặt tên property trùng với JS)
        var branches = await _context.Branches
            .Select(b => new
            {
                branchID = b.BranchID,   // viết thường để JS đọc đúng
                name = b.Name,
                city = b.City,
                latitude = b.Latitude,
                longitude = b.Longitude
            })
            .ToListAsync();

        // 🔹 Truyền sang View qua ViewBag
        ViewBag.Branches = branches;

        var cart = await _orderReadService.GetCartForCheckoutAsync(customerId.Value);
        return View(cart);
    }

    [HttpPost("CreateOrder")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> CreateOrder([FromForm] OrderFormModel form)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập" });
        }

        var res = await _checkoutService.CreateOrderOrStartMomoAsync(customerId.Value, form, HttpContext.Session);
        if (res.error != null) return Json(new { success = false, message = res.error });
        return Json(new { success = true, orderId = res.orderId, requireMomo = res.requireMomo });
    }

    [HttpPost("ValidatePromoCodes")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ValidatePromoCodes([FromBody] PromoValidationRequest request)
    {
        // Lấy userId nếu user đã đăng nhập
        int? customerId = GetCustomerId();
        if (customerId.HasValue)
        {
            request.UserId = customerId.Value;
        }
        
        var result = await _orderService.ValidateAndApplyPromoCodesAsync(request);
        return Json(result);
    }

    [HttpPost("ApplyDiscount")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountRequest request)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập để sử dụng mã giảm giá." });
        }

        try
        {
            var success = await _discountService.ApplyDiscountAsync(customerId.Value.ToString(), request.Code);
            
            if (success)
            {
                // Get discount details for response
                var discount = await _discountService.ValidateDiscountAsync(request.Code);
                if (discount != null)
                {
                    return Json(new { 
                        success = true, 
                        message = "Áp dụng mã giảm giá thành công!",
                        discount = new {
                            code = discount.Code,
                            percent = discount.Percent,
                            amount = discount.Amount
                        }
                    });
                }
            }
            
            return Json(new { success = false, message = "Không thể áp dụng mã giảm giá." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("OrderHistory")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> OrderHistory(int page = 1, int pageSize = 10)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var vm = await _orderReadService.GetOrderHistoryAsync(customerId.Value, page, pageSize);
        return View(vm);
    }
    #region Payment with MoMo
    [HttpGet("PayWithMomo")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> PayWithMomo()
    {
        // Lấy form đang chờ từ session
        var formJson = HttpContext.Session.GetString("PendingOrderForm");
        if (string.IsNullOrEmpty(formJson)) return BadRequest("Không có đơn hàng chờ thanh toán.");
        var form = System.Text.Json.JsonSerializer.Deserialize<OrderFormModel>(formJson);
        if (form == null) return BadRequest("Dữ liệu đơn hàng không hợp lệ.");

        // Ước tính tổng tiền để gửi qua MoMo (đồng bộ với tính toán ở OrderService)
        // Tạm thời dùng FinalTotal phía client submit (ShippingFee + itemsTotal - discounts) nếu có.
        // Ở đây đơn giản: lấy giỏ + tính lại trước khi redirect.
        int? customerId = GetCustomerId();
        if (customerId == null) return RedirectToAction("Login", "Account");

        var payUrl = await _checkoutService.InitiateMomoPaymentAsync(customerId.Value, HttpContext.Session, HttpContext);
        return Redirect(payUrl);
    }


    [HttpGet("PaymentCallback")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public async Task<IActionResult> PaymentCallback()
    {
        int? customerId = GetCustomerId();
        if (customerId == null) return RedirectToAction("Login", "Account");

        var result = await _checkoutService.HandleMomoCallbackAsync(Request.Query, customerId.Value, HttpContext.Session);
        if (!result.success) return RedirectToAction("Failed");
        return RedirectToAction("OrderConfirmed", new { id = result.orderId });
    }

    [HttpPost("RefundMomo/{orderId}")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefundMomo(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.TransId))
            return BadRequest("Không tìm thấy giao dịch để hoàn tiền.");

        // 🟢 Gọi API Refund MoMo
        var resultJson = await _paymentService.RefundAsync(order.TransId, order.Total, "Hoàn tiền đơn hàng");

        var response = System.Text.Json.JsonSerializer.Deserialize<MomoRefundResponse>(resultJson);
        if (response == null)
            return BadRequest("Không thể đọc phản hồi từ MoMo.");

        // 🟢 Nếu refund thành công
        if (response.resultCode == 0)
        {
            order.Status = "Đã hoàn tiền";
            order.RefundTransId = response.orderId;
            order.RefundAt = DateTime.Now;

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Refund thành công: {order.OrderCode} - TransId: {order.TransId}");
        }
        else
        {
            Console.WriteLine($"❌ Refund thất bại: {response.message}");
        }

        return RedirectToAction("OrderHistory", "Order");
    }

    #endregion
    [HttpGet("Failed")]
    public IActionResult OrderFailed()
    {
        ViewBag.Message = "Thanh toán thất bại, vui lòng thử lại.";
        return RedirectToAction("Order", "Order");
    }

    [HttpPost("Cancel/{id}")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, [FromForm] string? reason)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập" });
        }

        var result = await _orderService.CancelByCustomerAsync(id, customerId.Value, reason);
        return Json(new { success = result.success, message = result.message, cancelledAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), reason });
    }

    #region Reorder
    [HttpPost("Reorder")]
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public IActionResult Reorder([FromForm] int orderId)
    {
        int? customerId = GetCustomerId();
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        if (_orderService.Reorder(customerId.Value, orderId, out string message))
            return Json(new { success = true, redirectUrl = Url.Action("Order", "Order") });

        return Json(new { success = false, message });
    }
    #endregion
}


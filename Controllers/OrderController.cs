using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Models;
using start.Data;
using System.Security.Claims;

[Route("Order")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IOrderReadService _orderReadService;
    private readonly ICheckoutService _checkoutService;
    private readonly ApplicationDbContext _context;


    public OrderController(IOrderService orderService, IOrderReadService orderReadService, ICheckoutService checkoutService, ApplicationDbContext context)
    {
        _orderService = orderService;
        _orderReadService = orderReadService;
        _checkoutService = checkoutService;
        _context = context;
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
    public async Task<IActionResult> OrderConfirmed(int id)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
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
    public async Task<IActionResult> Order()
    {
        // Lấy ID khách hàng từ session
        int? customerId = HttpContext.Session.GetInt32("CustomerID");

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
    public async Task<IActionResult> CreateOrder([FromForm] OrderFormModel form)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
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
        var result = await _orderService.ValidateAndApplyPromoCodesAsync(request);
        return Json(result);
    }

    [HttpGet("OrderHistory")]
    public async Task<IActionResult> OrderHistory()
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var vm = await _orderReadService.GetOrderHistoryAsync(customerId.Value);
        return View(vm);
    }

    [HttpGet("PayWithMomo")]
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
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null) return RedirectToAction("Login", "Account");

        var payUrl = await _checkoutService.InitiateMomoPaymentAsync(customerId.Value, HttpContext.Session, HttpContext);
        return Redirect(payUrl);
    }

    [HttpGet("PaymentCallback")]
    public async Task<IActionResult> PaymentCallback()
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null) return RedirectToAction("Login", "Account");

        var result = await _checkoutService.HandleMomoCallbackAsync(Request.Query, customerId.Value, HttpContext.Session);
        if (!result.success) return RedirectToAction("Failed");
        return RedirectToAction("OrderConfirmed", new { id = result.orderId });
    }

    [HttpGet("Failed")]
    public IActionResult OrderFailed()
    {
        ViewBag.Message = "Thanh toán thất bại, vui lòng thử lại.";
        return RedirectToAction("Order", "Order");
    }

    [HttpPost("Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, [FromForm] string? reason)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập" });
        }

        var result = await _orderService.CancelByCustomerAsync(id, customerId.Value, reason);
        return Json(new { success = result.success, message = result.message, cancelledAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), reason });
    }

    #region Reorder
    [HttpPost("Reorder")]
    public IActionResult Reorder([FromForm] int orderId)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        if (_orderService.Reorder(customerId.Value, orderId, out string message))
            return Json(new { success = true, redirectUrl = Url.Action("Order", "Order") });

        return Json(new { success = false, message });
    }
    #endregion
}


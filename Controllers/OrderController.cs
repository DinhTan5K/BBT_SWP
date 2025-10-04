using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Models;
using start.Data;

[Route("Order")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _context;

    public OrderController(IOrderService orderService, ApplicationDbContext context)
    {
        _orderService = orderService;
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
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
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
            return RedirectToAction("Login", "Home");
        }

        // Lấy giỏ hàng kèm sản phẩm và size
        var cart = await _context.Carts
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.ProductSize)
            .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

        // Nếu không có giỏ, tạo giỏ trống
        if (cart == null)
        {
            cart = new Cart { CartDetails = new List<CartDetail>() };
        }

        return View(cart);
    }

    [HttpPost("CreateOrder")]
    public async Task<IActionResult> CreateOrder([FromForm] OrderFormModel form)
    {
        var result = await _orderService.CreateOrderAsync(form);

        if (!result.success)
            return Json(new { success = false, message = result.message });

        return Json(new { success = true, orderId = result.orderId });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Models;
using start.Data;
using System.Security.Claims;

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
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
        {
            return Json(new { success = false, message = "Bạn cần đăng nhập" });
        }

        var result = await _orderService.CreateOrderAsync(customerId.Value, form);

        if (!result.success)
            return Json(new { success = false, message = result.message });

        return Json(new { success = true, orderId = result.orderId });
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
        // === BƯỚC 1: LẤY DỮ LIỆU TỪ DATABASE ===

        // Lấy ID của khách hàng đang đăng nhập.
        // Cách lấy ID có thể khác nhau tùy theo cách bạn cài đặt authentication.
        // Đây là một cách phổ biến với ASP.NET Core Identity.
         int? customerId = HttpContext.Session.GetInt32("CustomerID");

        if (customerId == null)
        {
            // Nếu chưa login, cho về trang đăng nhập
            return RedirectToAction("Login", "Account");
        }

        // Truy vấn tất cả các đơn hàng của khách hàng đó.
        // Dùng Include và ThenInclude để lấy tất cả dữ liệu liên quan trong 1 lần gọi DB -> Tối ưu hiệu năng.
        // Sắp xếp theo ngày tạo mới nhất lên đầu.
        var customerOrders = await _context.Orders
            .Where(o => o.CustomerID == customerId)
            .Include(o => o.OrderDetails) // Lấy danh sách các OrderDetail
                .ThenInclude(od => od.Product) // Trong mỗi OrderDetail, lấy thông tin Product
            .Include(o => o.OrderDetails) // Lấy danh sách các OrderDetail một lần nữa
                .ThenInclude(od => od.ProductSize) // Trong mỗi OrderDetail, lấy thông tin ProductSize
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();


        // === BƯỚC 2: CHUYỂN ĐỔI (MAP) SANG VIEWMODEL ===
        // Đây là lúc "chế biến" dữ liệu thô thành "món ăn" đẹp đẽ cho View.

        var orderHistoryViewModel = new OrderHistoryViewModel
        {
            Orders = customerOrders.Select(order => new OrderSummaryViewModel
            {
                OrderID = order.OrderID,
                OrderCode = order.OrderCode,
                CreatedAt = order.CreatedAt,
                Total = order.Total,
                Status = order.Status,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                Address = order.Address,
                DetailAddress = order.DetailAddress,
                NoteOrder = order.NoteOrder,

                // Map danh sách các sản phẩm chi tiết
                OrderDetails = order.OrderDetails.Select(detail => new OrderDetailItemViewModel
                {
                    ProductName = detail.Product.ProductName, // Giả sử Product có thuộc tính Name
                    ProductImageUrl = detail.Product.Image_Url, // Giả sử Product có thuộc tính ImageUrl
                    SizeName = detail.ProductSize.Size, // Giả sử ProductSize có thuộc tính Name
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Total = detail.Total
                }).ToList()
            }).ToList()
        };


        // === BƯỚC 3: TRẢ VIEWMODEL VỀ CHO VIEW ===
        return View(orderHistoryViewModel);
    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;


public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }


    public IActionResult Product()
    {
        var products = _context.Products
    .Include(p => p.ProductSizes)
    .ToList();

        return View(products);
    }


    [HttpPost]
    public IActionResult UpdateCart([FromBody] UpdateCartRequest request)
    {
        if (request == null || request.Quantity < 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefault(c => c.CustomerID == customerId.Value);

        if (cart == null)
            return Json(new { success = false, message = "Cart không tồn tại" });

        var cartDetail = cart.CartDetails.FirstOrDefault(cd => cd.CartDetailID == request.CartDetailId);
        if (cartDetail == null)
            return Json(new { success = false, message = "Cart item không tồn tại" });

        if (request.Quantity == 0)
        {
            _context.CartDetails.Remove(cartDetail);
        }
        else
        {
            cartDetail.Quantity = request.Quantity;
            cartDetail.Total = cartDetail.UnitPrice * cartDetail.Quantity;
        }

        _context.SaveChanges();

        return Json(new { success = true });
    }


    [HttpGet]
    public IActionResult CartItems()
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Chưa đăng nhập" });

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .ThenInclude(cd => cd.Product)
            .Include(c => c.CartDetails)
            .ThenInclude(cd => cd.ProductSize)
            .FirstOrDefault(c => c.CustomerID == customerId.Value);

        if (cart == null || !cart.CartDetails.Any())
            return Json(new List<object>());

        var items = cart.CartDetails.Select(cd => new
        {
            cd.CartDetailID,
            cd.ProductID,
            ProductName = cd.Product?.ProductName ?? "Unknown",
            cd.ProductSizeID,
            Size = cd.ProductSize?.Size ?? "N/A",
            cd.Quantity,
            cd.UnitPrice,
            cd.Total
        }).ToList();

        return Json(items);
    }

    [HttpPost]
    public IActionResult AddToCart([FromBody] AddToCartRequest request)
    {
        if (request == null || request.Quantity <= 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefault(c => c.CustomerID == customerId.Value);

        if (cart == null)
        {
            cart = new Cart
            {
                CustomerID = customerId.Value,
                CreatedAt = DateTime.Now,
                CartDetails = new List<CartDetail>()
            };
            _context.Carts.Add(cart);
            _context.SaveChanges();
        }

        var size = _context.ProductSizes
            .FirstOrDefault(ps => ps.ProductSizeID == request.ProductSizeId);

        if (size == null)
            return Json(new { success = false, message = "Size không tồn tại" });

        var existing = cart.CartDetails
            .FirstOrDefault(cd => cd.ProductID == request.ProductId
                               && cd.ProductSizeID == request.ProductSizeId);

        if (existing != null)
        {
            existing.Quantity += request.Quantity;
            existing.Total = existing.UnitPrice * existing.Quantity;
        }
        else
        {
            cart.CartDetails.Add(new CartDetail
            {
                ProductID = request.ProductId,
                ProductSizeID = request.ProductSizeId,
                Quantity = request.Quantity,
                UnitPrice = size.Price,
                Total = size.Price * request.Quantity
            });
        }

        _context.SaveChanges();

        return Json(new { success = true });
    }

    
     
}


public class AddToCartRequest
{
    public int ProductId { get; set; }
    public int ProductSizeId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Price { get; set; }
}
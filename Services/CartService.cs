using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<object> GetCartItems(int customerId)
    {
        var cart = _context.Carts
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.ProductSize)
            .FirstOrDefault(c => c.CustomerID == customerId);

        if (cart == null || !cart.CartDetails.Any())
            return new List<object>();

        return cart.CartDetails.Select(cd => new
        {
            cd.CartDetailID,
            cd.ProductID,
            ProductName = cd.Product?.ProductName ?? "Unknown",
            cd.ProductSizeID,
            Size = cd.ProductSize?.Size ?? "N/A",
            cd.Quantity,
            cd.UnitPrice,
            cd.Total
        }).Cast<object>().ToList();
    }

    public bool AddToCart(int customerId, AddToCartRequest request, out string message)
    {
        message = "";

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefault(c => c.CustomerID == customerId);

        if (cart == null)
        {
            cart = new Cart
            {
                CustomerID = customerId,
                CreatedAt = DateTime.Now,
                CartDetails = new List<CartDetail>()
            };
            _context.Carts.Add(cart);
            _context.SaveChanges();
        }

        var size = _context.ProductSizes
            .FirstOrDefault(ps => ps.ProductSizeID == request.ProductSizeId);

        if (size == null)
        {
            message = "Size không tồn tại";
            return false;
        }

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
        return true;
    }

    public bool UpdateCart(int customerId, UpdateCartRequest request, out string message)
    {
        message = "";

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefault(c => c.CustomerID == customerId);

        if (cart == null)
        {
            message = "Cart không tồn tại";
            return false;
        }

        var cartDetail = cart.CartDetails
            .FirstOrDefault(cd => cd.CartDetailID == request.CartDetailId);

        if (cartDetail == null)
        {
            message = "Cart item không tồn tại";
            return false;
        }

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
        return true;
    }

    public bool Reorder(int customerId, int orderId, out string message)
    {
        message = "";

        var order = _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefault(o => o.OrderID == orderId && o.CustomerID == customerId);

        if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
        {
            message = "Đơn hàng không tồn tại hoặc không có sản phẩm";
            return false;
        }

        if (string.Equals(order.Status, "Chờ xác nhận", StringComparison.OrdinalIgnoreCase))
        {
            message = "Đơn đang chờ xác nhận, không thể đặt lại.";
            return false;
        }

        var cart = _context.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefault(c => c.CustomerID == customerId);

        if (cart == null)
        {
            cart = new Cart
            {
                CustomerID = customerId,
                CreatedAt = DateTime.Now,
                CartDetails = new List<CartDetail>()
            };
            _context.Carts.Add(cart);
            _context.SaveChanges();
        }

        foreach (var od in order.OrderDetails)
        {
            // Lấy giá hiện tại theo ProductSize, fallback dùng UnitPrice cũ
            var size = _context.ProductSizes.FirstOrDefault(ps => ps.ProductSizeID == od.ProductSizeID);
            var unitPrice = size?.Price ?? od.UnitPrice;

            var existing = cart.CartDetails
                .FirstOrDefault(cd => cd.ProductID == od.ProductID && cd.ProductSizeID == od.ProductSizeID);

            if (existing != null)
            {
                existing.Quantity += od.Quantity;
                existing.Total = existing.UnitPrice * existing.Quantity;
            }
            else
            {
                cart.CartDetails.Add(new CartDetail
                {
                    ProductID = od.ProductID,
                    ProductSizeID = od.ProductSizeID,
                    Quantity = od.Quantity,
                    UnitPrice = unitPrice,
                    Total = unitPrice * od.Quantity
                });
            }
        }

        _context.SaveChanges();
        return true;
    }
}

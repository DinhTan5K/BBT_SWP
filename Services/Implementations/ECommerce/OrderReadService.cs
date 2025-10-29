using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using start.Data;
using start.Models;

public class OrderReadService : IOrderReadService
{
    private readonly ApplicationDbContext _context;

    public OrderReadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> GetCartForCheckoutAsync(int customerId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
            .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.ProductSize)
            .FirstOrDefaultAsync(c => c.CustomerID == customerId);

        return cart ?? new Cart { CartDetails = new List<CartDetail>() };
    }

    public async Task<OrderHistoryViewModel> GetOrderHistoryAsync(int customerId)
    {
        var customerOrders = await _context.Orders
            .Where(o => o.CustomerID == customerId)
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.ProductSize)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new OrderHistoryViewModel
        {
            Orders = customerOrders.Select(order => new OrderSummaryViewModel
            {
                OrderID = order.OrderID,
                OrderCode = order.OrderCode,
                CreatedAt = order.CreatedAt,
                Total = order.Total,
                Status = order.Status,
                CancelledAt = order.CancelledAt,
                CancelReason = order.CancelReason,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                Address = order.Address,
                DetailAddress = order.DetailAddress,
                NoteOrder = order.NoteOrder,
                OrderDetails = order.OrderDetails.Select(detail => new OrderDetailItemViewModel
                {
                    ProductName = detail.Product.ProductName,
                    ProductImageUrl = detail.Product.Image_Url,
                    SizeName = detail.ProductSize.Size,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Total = detail.Total
                }).ToList()
            }).ToList()
        };

        return vm;
    }
}



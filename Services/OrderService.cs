using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    private string GenerateOrderCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 5)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task<(bool success, string message, int? orderId)> CreateOrderAsync(OrderFormModel form)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var branch = await _context.Branches.FindAsync(form.BranchID);
            if (branch == null)
                return (false, "Chi nhánh không tồn tại", null);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == form.Phone);

            if (customer == null)
            {
                customer = new Customer
                {
                    Name = form.Name,
                    Phone = form.Phone,
                    Address = form.Address
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                BranchID = form.BranchID,
                CreatedAt = DateTime.Now,
                Status = "Chờ xác nhận",
                OrderCode = GenerateOrderCode(),
                Total = 0
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.CustomerID == customer.CustomerID);

            if (cart == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống", null);

            var orderDetails = cart.CartDetails.Select(cd => new OrderDetail
            {
                OrderID = order.OrderID,
                ProductID = cd.ProductID,
                ProductSizeID = cd.ProductSizeID,
                Quantity = cd.Quantity,
                UnitPrice = cd.UnitPrice,
                Total = cd.UnitPrice * cd.Quantity
            }).ToList();

            _context.OrderDetails.AddRange(orderDetails);

            order.Total = orderDetails.Sum(d => d.Total);
            _context.Orders.Update(order);

            _context.CartDetails.RemoveRange(cart.CartDetails);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Tạo order thành công", order.OrderID);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message, null);
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product!)
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.ProductSize)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderID == id);
    }

    public async Task<object?> GetOrderByCodeAsync(string orderCode)
    {
        return await _context.Orders
            .Where(o => o.OrderCode == orderCode)
            .Select(o => new
            {
                o.OrderID,
                o.CustomerID,
                o.BranchID,
                o.CreatedAt,
                o.OrderCode,
                o.Status,
                o.Total
            })
            .FirstOrDefaultAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;

namespace start.Services
{
    public class ShipperService : IShipperService
    {
        private readonly ApplicationDbContext _context;

        public ShipperService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📦 Lấy đơn hàng trong ca làm hiện tại
        public async Task<List<Order>> GetMyOrdersAsync(string shipperId)
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == shipperId);
            if (emp == null || emp.BranchID == null)
                return new List<Order>();

            int branchId = emp.BranchID.Value;
            var today = DateTime.Today;

            // 🔹 Xác định ca làm
            var work = await _context.WorkSchedules
                .FirstOrDefaultAsync(w => w.EmployeeID == shipperId && w.WorkDate == today && w.IsActive);
            if (work == null)
                return new List<Order>();

            var (startTime, endTime) = GetShiftRange(today, work.Shift);

            // 🔹 Lọc đơn trong chi nhánh và ca làm hiện tại
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.BranchID == branchId &&
                            o.CreatedAt >= startTime &&
                            o.CreatedAt <= endTime &&
                            (o.Status == "Đã xác nhận" || o.Status == "Đang giao"))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        // 🚚 Cập nhật trạng thái đơn hàng theo ca làm
        public async Task<string> UpdateOrderStatusAsync(int id, string status, string empId)
        {
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == empId);
            if (emp == null)
                return "❌ Không tìm thấy nhân viên.";

            var today = DateTime.Today;
            var work = await _context.WorkSchedules
                .FirstOrDefaultAsync(w => w.EmployeeID == empId && w.WorkDate == today && w.IsActive);
            if (work == null)
                return "⚠️ Bạn không có ca làm hôm nay.";

            var (startTime, endTime) = GetShiftRange(today, work.Shift);

            var order = await _context.Orders.FirstOrDefaultAsync(o =>
                o.OrderID == id &&
                o.BranchID == emp.BranchID &&
                o.CreatedAt >= startTime &&
                o.CreatedAt <= endTime);

            if (order == null)
                return "❌ Không tìm thấy đơn hàng trong ca làm của bạn.";

            switch (status)
            {
                case "Đang giao":
                    if (order.Status == "Đã xác nhận")
                    {
                        order.Status = "Đang giao";
                        await _context.SaveChangesAsync();
                        return $"✅ Bạn đã nhận giao đơn {order.OrderCode}.";
                    }
                    return "⚠️ Đơn này không thể nhận giao.";

                case "Delivered":
                    if (order.Status == "Đang giao")
                    {
                        order.Status = "Đã giao";
                        await _context.SaveChangesAsync();
                        return $"✅ Đơn {order.OrderCode} đã giao thành công.";
                    }
                    return "⚠️ Chỉ đơn đang giao mới có thể hoàn tất.";

                case "Cancelled":
                    if (order.Status == "Đang giao" || order.Status == "Đã xác nhận")
                    {
                        order.Status = "Đã hủy";
                        order.CancelReason = "Shipper hủy đơn.";
                        order.CancelledAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                        return $"❌ Đơn {order.OrderCode} đã bị hủy.";
                    }
                    return "⚠️ Đơn không thể hủy trong trạng thái này.";

                default:
                    return "❌ Trạng thái không hợp lệ.";
            }
        }

        // 🔧 Hàm phụ giống InternalController
        private static (DateTime start, DateTime end) GetShiftRange(DateTime today, string? shift)
        {
            if (string.Equals(shift, "Morning", StringComparison.OrdinalIgnoreCase))
                return (today.AddHours(0), today.AddHours(14).AddMinutes(59).AddSeconds(59));

            return (today.AddHours(15), today.AddHours(23).AddMinutes(59).AddSeconds(59));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using start.Services;
using start.Data;

namespace start.Controllers
{
    public class ShipperController : Controller
    {
        private readonly IShipperService _service;
        private readonly ApplicationDbContext _db;

        public ShipperController(IShipperService service,  ApplicationDbContext db)
        {
            _service = service;
            _db = db;
        }

        // 📦 GET: /Shipper/MyOrders
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var empId = HttpContext.Session.GetString("EmployeeID");
            var role = HttpContext.Session.GetString("RoleID");

            if (string.IsNullOrEmpty(empId) || role != "SH")
                return RedirectToAction("Login", "Account");

            var orders = await _service.GetMyOrdersAsync(empId);
            if (!orders.Any())
            {
                ViewBag.Message = "❌ Bạn không có đơn hàng trong ca hiện tại.";
            }
                var emp = await _db.Employees.FindAsync(empId);
ViewBag.Employee = emp;


            return View("~/Views/Shipper/MyOrders.cshtml", orders);
        }

        // 🚀 POST: /Shipper/UpdateStatus
    [HttpPost]
[IgnoreAntiforgeryToken]

public async Task<IActionResult> UpdateStatus(int id, string status)
{
    try
    {
        var empId = HttpContext.Session.GetString("EmployeeID");
        if (string.IsNullOrEmpty(empId))
            return RedirectToAction("Login", "Account");

        var message = await _service.UpdateOrderStatusAsync(id, status, empId);
TempData["shipper_ok"] = message; // ✅ đổi key để không trùng
return RedirectToAction("MyOrders");

    }
    catch (Exception ex)
    {
        TempData["ok"] = "⚠️ Lỗi: " + ex.Message;
        return RedirectToAction("MyOrders");
    }
}


        // 🔹 Request model cho fetch JSON
        public class UpdateStatusRequest
        {
            public int id { get; set; }
            public string status { get; set; } = "";
        }
    }
}

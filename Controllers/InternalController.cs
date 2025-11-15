using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Services;
using ClosedXML.Excel;

namespace start.Controllers
{
    [Authorize(AuthenticationSchemes = "ShiftLeaderScheme")]
    public class InternalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ILogger<InternalController> _logger;           
        private readonly RevenueService _revenue;
        private readonly ShiftService _shift;
        private readonly SessionService _session;

        public InternalController(
            ApplicationDbContext context,
            IOrderService orderService, 
            ILogger<InternalController> logger,
            RevenueService revenue,
            ShiftService shift,
            SessionService session)
        {
            _context = context;
             _orderService = orderService;
            _logger = logger;
            _revenue = revenue;
            _shift = shift;
            _session = session;
        }

        // ============================================
        // 1️⃣ BRANCH ORDERS – Dashboard chính
        // ============================================
        [HttpGet]
        public async Task<IActionResult> BranchOrders()
        {
            var branchId = _session.GetBranchId();
            var employeeId = _session.GetEmployeeId();

            if (branchId == null || employeeId == null)
                return RedirectToAction("Login", "Account");

            var today = DateTime.Today;
            // 🔹 Lấy tên cơ sở từ bảng Branch
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchID == branchId);
            ViewBag.BranchName = branch?.Name ?? "Không rõ cơ sở";
            // 🔹 Đọc ca làm việc hiện tại của trưởng ca
            var work = await _context.WorkSchedules
                .FirstOrDefaultAsync(w => w.EmployeeID == employeeId && w.Date == today && w.IsActive);

            if (work == null)
            {
                ViewBag.Shift = "None";
                ViewBag.Date = today.ToString("dd/MM/yyyy");
                return View("~/Views/Internal/Internal.cshtml", new List<Order>());
            }

            string shiftName = work.Shift == "Sáng" ? "Morning" : "Night";

            var (shift, start, end) = _shift.GetShift(today, shiftName);

            HttpContext.Session.SetString("SelectedShift", shift);

            var (orders, productStats, summary) =
                await _revenue.GetRevenueAsync(branchId.Value, start, end);

            ViewBag.Shift = shift;
            ViewBag.Date = today.ToString("dd/MM/yyyy");


            // === Thêm phần tính thống kê doanh thu ===
            var deliveredOrdersInShift = orders.Where(o => o.Status == "Đã giao").ToList();

            var totalOrders = orders.Count;
            var completed = deliveredOrdersInShift.Count;
            var delivering = orders.Count(o => o.Status == "Đang giao");
            var cancelled = orders.Count(o => o.Status == "Đã hủy");
            var totalRevenue = deliveredOrdersInShift
                .Select(o => (decimal?)o.Total ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            // 🔍 Thống kê chi tiết sản phẩm bán ra (theo số lượng)
            // Sửa: Chỉ tính trên các đơn hàng đã giao trong ca
            productStats = deliveredOrdersInShift
                .Where(o => o.OrderDetails != null)
                .SelectMany(o => o.OrderDetails!)
                .Where(od => od.Product != null)
                .GroupBy(od => od.Product!.ProductName)
                .Select(g => new start.Services.ProductStat // Sửa: Tạo đối tượng ProductStat thay vì anonymous type
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ToList();

            // Tổng số lượng bán ra để tính phần trăm
            var totalQuantity = productStats.Sum(x => x.Quantity);

            // Gửi dữ liệu qua ViewBag để hiển thị
            ViewBag.TotalQuantity = totalQuantity;
            ViewBag.ProductStats = productStats;
            ViewBag.TotalOrders = summary.TotalOrders;
            ViewBag.Completed = summary.Completed;
            ViewBag.Delivering = summary.Delivering;
            ViewBag.Cancelled = summary.Cancelled;
            ViewBag.TotalRevenue = summary.TotalRevenue;

            // 🔹 Gửi data cho ChartJS (ở dạng JSON)
            ViewBag.ProductChartData = System.Text.Json.JsonSerializer.Serialize(productStats);



            // 🕒 Gom doanh thu theo từng khoảng 30 phút
            // Sửa: Chỉ tính trên các đơn hàng đã giao trong ca và dùng UpdatedAt
            var intervalRevenue = deliveredOrdersInShift
                .Where(o => o.UpdatedAt.HasValue)
                .GroupBy(o =>
                {
                    var time = o.UpdatedAt.Value;
                    int roundedMinutes = (time.Minute / 30) * 30; // 0 hoặc 30 phút
                    return new DateTime(time.Year, time.Month, time.Day, time.Hour, roundedMinutes, 0);
                })
                .Select(g => new
                {
                    TimeSlot = g.Key,
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.TimeSlot)
                .ToList();

            // 🔹 Serialize cho Chart.js
            ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(
                intervalRevenue.Select(x => new
                {
                    Label = x.TimeSlot.ToString("HH:mm"),
                    x.Revenue
                })
            );

            return View("~/Views/Internal/Internal.cshtml", orders);
        }


        // ============================================
        // 2️⃣ Xác nhận đơn
        // ============================================
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            // Sử dụng OrderService để đảm bảo logic được đồng bộ (cập nhật UpdatedAt)
            var (success, message) = await _orderService.UpdateOrderStatusAsync(id, "Đã xác nhận");

            if (success)
            {
                TempData["Message"] = $"Đơn hàng đã được xác nhận thành công.";
            }
            else
            {
                TempData["Error"] = message; // Hiển thị lỗi nếu có
            }

            var branchId = _session.GetBranchId();
            if (branchId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order == null || order.BranchID != branchId)
                return NotFound();

            order.Status = "Đã xác nhận";
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Đơn {order.OrderCode} đã được xác nhận";
            return RedirectToAction("BranchOrders");
        }

        // ============================================
        // 3️⃣ Xem chi tiết đơn hàng (Popup)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var branchId = _session.GetBranchId();
            if (branchId == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.ProductSize)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.BranchID == branchId);

            if (order == null) return NotFound();

            return PartialView("OrderDetailsModal", order);
        }

        // ============================================
        // 4️⃣ Xem chi tiết đơn hàng (View)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> OrderDetailsView(int id)
        {
            var branchId = _session.GetBranchId();
            if (branchId == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.ProductSize)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.BranchID == branchId);

            if (order == null) return NotFound();

            return PartialView("_OrderDetailsView", order);
        }

        [HttpPost]
        public async Task<IActionResult> DeliverOrder(int orderId)
        {
            var branchId = HttpContext.Session.GetInt32("BranchId");
            if (!branchId.HasValue)
            {
                return Json(new { success = false, message = "Không xác định được chi nhánh." });
            }

            // 1. Tìm một Shipper đang trong ca làm việc (đã check-in và ca đã được duyệt)
            var now = DateTime.Now;
            var availableShipper = await _context.WorkSchedules
                .Where(ws => ws.Employee.BranchID == branchId.Value &&
                             ws.Employee.RoleID == "SP" &&
                             ws.Date.Date == now.Date && // Lịch làm việc của ngày hôm nay
                             ws.Status == "Đã duyệt" && // SỬA LỖI: Bổ sung điều kiện ca làm phải được duyệt
                             ws.CheckInTime.HasValue && // Shipper phải đã check-in
                             !ws.CheckOutTime.HasValue) // và chưa check-out
                .Select(ws => ws.Employee)
                .FirstOrDefaultAsync();

            if (availableShipper == null)
            {
                return Json(new { success = false, message = "Không có shipper nào sẵn sàng trong ca làm việc hiện tại." });
            }

            // 2. Nếu có shipper, gán đơn hàng và cập nhật trạng thái
            var (success, message) = await _orderService.UpdateOrderStatusAsync(orderId, "Đang giao", availableShipper.EmployeeID);

            if (!success) return Json(new { success = false, message });
            return Json(new { success = true, message = "Đơn hàng đã được chuyển sang trạng thái 'Đang giao'." });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            // Sử dụng OrderService để đảm bảo logic được đồng bộ (cập nhật UpdatedAt)
            var (success, message) = await _orderService.UpdateOrderStatusAsync(orderId, "Đã giao");

            if (!success) return Json(new { success = false, message });
            return Json(new { success = true, message = "Đơn hàng đã được hoàn tất thành công." });
        }


        private string? GetCurrentShiftName(DateTime now)
        {
            var t = now.TimeOfDay;
            if (t < new TimeSpan(15, 0, 0)) return "Sáng";
            if (t < new TimeSpan(24, 0, 0)) return "Tối";
            return null;
        }

        // ============================================
        // 6️⃣ Lấy nhân viên trong ca
        // ============================================
        public IActionResult GetEmployeesInCurrentShift()
        {
            var branchId = _session.GetBranchId();
            if (branchId == null)
                return Unauthorized("Không xác định chi nhánh");

            var shiftName = _shift.GetCurrentShift();
            if (shiftName == null)
                return PartialView("EmployeesInShiftPartial", new List<Employee>());
                 var currentShift = GetCurrentShiftName(DateTime.Now);
            if (currentShift == null)
                return PartialView("EmployeesInShiftPartial", new List<Employee>());

            var today = DateTime.Today;

            var employees = _context.WorkSchedules
                .Include(w => w.Employee)
                .Where(w => w.Date.Date == today
                         && w.Shift == currentShift
                         && w.Employee.BranchID == branchId)
                .Select(w => w.Employee!)
                .Distinct()
                .ToList();

            ViewBag.CurrentShift = shiftName;
            return PartialView("EmployeesInShiftPartial", employees);
        }

        // ============================================
        // 7️⃣ Thêm thưởng/phạt
        // ============================================
        [HttpPost]
        public async Task<IActionResult> AddSalaryAdjustment([FromBody] SalaryAdjustmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi dữ liệu: " + string.Join("; ", errors)
                });
            }

            var exists = await _context.Employees.AnyAsync(e => e.EmployeeID == dto.EmployeeID);
            if (!exists)
                return NotFound(new { success = false, message = "Nhân viên không tồn tại" });

            var amount = dto.Type == "Penalty"
                ? -Math.Abs(dto.Amount)
                : Math.Abs(dto.Amount);

            var adjustment = new SalaryAdjustment
            {
                EmployeeID = dto.EmployeeID,
                AdjustmentDate = DateTime.Now,
                Amount = amount,
                Reason = dto.Reason
            };

            _context.SalaryAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = dto.Type == "Penalty"
                    ? $"❌ Đã ghi nhận phạt {Math.Abs(amount):N0}đ"
                    : $"✅ Đã thưởng {amount:N0}đ"
            });
        }

        // ============================================
        // 8️⃣ Shift Report – dùng RevenueService
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ShiftReport()
        {
            var branchId = _session.GetBranchId();
            var employeeId = _session.GetEmployeeId();

            if (branchId == null || employeeId == null)
                return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            var work = await _context.WorkSchedules
                .FirstOrDefaultAsync(w => w.EmployeeID == employeeId && w.Date == today && w.IsActive);

            if (work == null)
                return View("~/Views/Internal/ShiftReport.cshtml", new List<Order>());

            string shiftName = work.Shift == "Sáng" ? "Morning" : "Night";

            var (shift, start, end) = _shift.GetShift(today, shiftName);

            var (orders, productStats, summary) =
                await _revenue.GetRevenueAsync(branchId.Value, start, end);

            ViewBag.Shift = shift;
            ViewBag.Date = today.ToString("dd/MM/yyyy");
            ViewBag.ProductStats = productStats;
            ViewBag.TotalOrders = summary.TotalOrders;
            ViewBag.Completed = summary.Completed;
            ViewBag.Delivering = summary.Delivering;
            ViewBag.Cancelled = summary.Cancelled;
            ViewBag.TotalRevenue = summary.TotalRevenue;
            ViewBag.ProductChartData = Newtonsoft.Json.JsonConvert.SerializeObject(productStats);
            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(
                _revenue.GetRevenueChart(orders)
            );
            return View("~/Views/Internal/ShiftReport.cshtml", orders);
        }

        // ============================================
        // 9️⃣ Export Excel
        // ============================================
        [HttpGet]
        public async Task<IActionResult> ExportRevenueToExcel()
        {
            var branchId = _session.GetBranchId();
            var employeeId = _session.GetEmployeeId();
            var leaderName = _session.GetEmployeeName();

            if (branchId == null || employeeId == null)
                return RedirectToAction("Login", "Account");

            var branch = await _context.Branches.FindAsync(branchId);
            string branchName = branch?.Name ?? "Không rõ cơ sở";

            var today = DateTime.Today;

            var shiftSessionName = HttpContext.Session.GetString("SelectedShift") ?? "Morning";
            var (shift, start, end) = _shift.GetShift(today, shiftSessionName);

            // 🔥 Lấy dữ liệu doanh thu
            var (orders, productStats, summary) =
                await _revenue.GetRevenueAsync(branchId.Value, start, end);

            // 🔥 Lấy THƯỞNG / PHẠT theo ca
            var adjustments = await _context.SalaryAdjustments
                .Include(a => a.Employee)
                .Where(a => a.AdjustmentDate >= start && a.AdjustmentDate <= end)
                .OrderByDescending(a => a.AdjustmentDate)
                .ToListAsync();

            // ======================================
            //         TẠO FILE EXCEL
            // ======================================
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Báo cáo doanh thu");

            ws.Cell("A1").Value = "BÁO CÁO DOANH THU";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Range("A1:C1").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A3").Value = "Ngày:";
            ws.Cell("B3").Value = today.ToString("dd/MM/yyyy");

            ws.Cell("A4").Value = "Ca làm:";
            ws.Cell("B4").Value = shift == "Morning" ? "Ca sáng" : "Ca tối";

            ws.Cell("A5").Value = "Trưởng ca:";
            ws.Cell("B5").Value = leaderName;

            ws.Cell("A6").Value = "Cơ sở:";
            ws.Cell("B6").Value = branchName;

            // ======================================
            //          TỔNG QUAN DOANH THU
            // ======================================
            ws.Cell("A7").Value = "Chỉ tiêu";
            ws.Cell("B7").Value = "Giá trị";
            ws.Range("A7:B7").Style.Font.Bold = true;
            ws.Range("A7:B7").Style.Fill.BackgroundColor = XLColor.LightGreen;

            var summaryList = new List<(string Label, object Value)>
    {
        ("Tổng đơn hàng", summary.TotalOrders),
        ("Đơn hoàn tất", summary.Completed),
        ("Đơn đang giao", summary.Delivering),
        ("Đơn hủy", summary.Cancelled),
        ("Tổng doanh thu (₫)", summary.TotalRevenue)
    };

            int row = 8;
            foreach (var item in summaryList)
            {
                ws.Cell(row, 1).Value = item.Label;
                ws.Cell(row, 2).Value = item.Value switch
                {
                    int v => v,
                    double v => v,
                    decimal v => v,
                    _ => item.Value?.ToString() ?? ""
                };
                row++;
            }

            // ======================================
            //           SẢN PHẨM BÁN RA
            // ======================================
            row += 2;
            ws.Cell(row, 1).Value = "SẢN PHẨM BÁN RA";
            ws.Range(row, 1, row, 3).Merge().Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = "Tên sản phẩm";
            ws.Cell(row, 2).Value = "Số lượng";
            ws.Cell(row, 3).Value = "Doanh thu (₫)";
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            row++;

            foreach (var p in productStats)
            {
                ws.Cell(row, 1).Value = p.ProductName;
                ws.Cell(row, 2).Value = p.Quantity;
                ws.Cell(row, 3).Value = p.Revenue;
                row++;
            }

            // ======================================
            //         THỐNG KÊ THƯỞNG / PHẠT
            // ======================================
            row += 2;
            ws.Cell(row, 1).Value = "THỐNG KÊ THƯỞNG / PHẠT TRONG CA";
            ws.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
            row++;

            ws.Cell(row, 1).Value = "Nhân viên";
            ws.Cell(row, 2).Value = "Loại";
            ws.Cell(row, 3).Value = "Lý do";
            ws.Cell(row, 4).Value = "Số tiền (₫)";
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            row++;

            if (adjustments.Any())
            {
                foreach (var adj in adjustments)
                {
                    ws.Cell(row, 1).Value = adj.Employee?.FullName ?? "Không rõ";
                    ws.Cell(row, 2).Value = adj.Amount >= 0 ? "Thưởng" : "Phạt";
                    ws.Cell(row, 3).Value = adj.Reason;
                    ws.Cell(row, 4).Value = adj.Amount;
                    row++;
                }
            }
            else
            {
                ws.Cell(row, 1).Value = "Không có thưởng/phạt trong ca";
                ws.Range(row, 1, row, 4).Merge();
                row++;
            }

            // ======================================
            //       TỰ ĐỘNG CĂN CHỈNH
            // ======================================
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string safeBranch = new string(branchName
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
                .ToArray());

            string fileName = $"BaoCao_DoanhThu_{safeBranch}_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

    }
}

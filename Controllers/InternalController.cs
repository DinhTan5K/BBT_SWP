using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.Services;
using ClosedXML.Excel;

namespace start.Controllers
{
    public class InternalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RevenueService _revenue;
        private readonly ShiftService _shift;
        private readonly SessionService _session;

        public InternalController(
            ApplicationDbContext context,
            RevenueService revenue,
            ShiftService shift,
            SessionService session)
        {
            _context = context;
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

            var work = await _context.WorkSchedules.FirstOrDefaultAsync(w =>
                w.EmployeeID == employeeId &&
                w.WorkDate == today &&
                w.IsActive);

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
            ViewBag.ProductStats = productStats;
            ViewBag.TotalOrders = summary.TotalOrders;
            ViewBag.Completed = summary.Completed;
            ViewBag.Delivering = summary.Delivering;
            ViewBag.Cancelled = summary.Cancelled;
            ViewBag.TotalRevenue = summary.TotalRevenue;

            // 🔥 THÊM 2 DÒNG QUYẾT ĐỊNH ĐỜI SỐNG 2 CÁI CHART
            ViewBag.ProductChartData = Newtonsoft.Json.JsonConvert.SerializeObject(productStats);
            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(
                _revenue.GetRevenueChart(orders)
            );

            return View("~/Views/Internal/Internal.cshtml", orders);
        }


        // ============================================
        // 2️⃣ Xác nhận đơn
        // ============================================
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
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

        // ============================================
        // 5️⃣ Hoàn tất đơn
        // ============================================
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return Json(new { success = false, message = "❌ Không tìm thấy đơn hàng." });

            order.Status = "Đã giao";
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                next = "Done",
                message = "Đơn hàng đã hoàn tất thành công!"
            });
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

            var today = DateTime.Today;

            var employees = _context.WorkSchedules
                .Include(w => w.Employee)
                .Where(w => w.WorkDate.Date == today &&
                            w.Shift == shiftName &&
                            w.Employee.BranchID == branchId)
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

            var work = await _context.WorkSchedules.FirstOrDefaultAsync(w =>
                w.EmployeeID == employeeId &&
                w.WorkDate == today &&
                w.IsActive);

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

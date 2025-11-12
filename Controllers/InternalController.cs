using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using ClosedXML.Excel;
namespace start.Controllers
{
    public class InternalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InternalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Internal/BranchOrders
        [HttpGet]
        public async Task<IActionResult> BranchOrders()
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;
            var employeeId = HttpContext.Session.GetString("EmployeeID");
            if (branchId == null || string.IsNullOrEmpty(employeeId))
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
                ModelState.AddModelError("", "❌ Bạn không có lịch làm việc hôm nay.");
                ViewBag.Shift = "None";
                ViewBag.Date = today.ToString("dd/MM/yyyy");
                return View("~/Views/Internal/Internal.cshtml", new List<Order>());
            }

            string shift = work.Shift?.Trim() ?? "Sáng";

            if (shift.Equals("Sáng", StringComparison.OrdinalIgnoreCase))
                shift = "Morning";
            else if (shift.Equals("Tối", StringComparison.OrdinalIgnoreCase))
                shift = "Night";

            // Lưu vào session + ViewBag
            HttpContext.Session.SetString("SelectedShift", shift);
            ViewBag.Shift = shift;// lưu lại để báo cáo đọc

            DateTime startTime, endTime;
            if (shift.Equals("Morning", StringComparison.OrdinalIgnoreCase))
            {
                startTime = today.AddHours(0);
                endTime = today.AddHours(14).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                startTime = today.AddHours(15);
                endTime = today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            var orders = await _context.Orders
       .Include(o => o.Customer)
       .Include(o => o.OrderDetails!)
           .ThenInclude(od => od.Product)
       .Where(o => o.BranchID == branchId &&
                   o.CreatedAt >= startTime &&
                   o.CreatedAt <= endTime)
       .ToListAsync();


            ViewBag.Shift = shift;
            ViewBag.Date = today.ToString("dd/MM/yyyy");


            // === Thêm phần tính thống kê doanh thu ===
            var totalOrders = orders.Count;
            var completed = orders.Count(o => o.Status == "Đã giao");
            var delivering = orders.Count(o => o.Status == "Đang giao");
            var cancelled = orders.Count(o => o.Status == "Đã hủy");
            var totalRevenue = orders
                .Where(o => o.Status == "Đã giao")
                .Select(o => (decimal?)o.Total ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            // 🔍 Thống kê chi tiết sản phẩm bán ra (theo số lượng)
            var productStats = orders
                .Where(o => o.Status == "Đã giao" && o.OrderDetails != null)
                .SelectMany(o => o.OrderDetails!)
                .Where(od => od.Product != null)
                .GroupBy(od => od.Product!.ProductName)
                .Select(g => new
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

            // 🔹 Gửi data cho ChartJS (ở dạng JSON)
            ViewBag.ProductChartData = System.Text.Json.JsonSerializer.Serialize(productStats);



            // 🕒 Gom doanh thu theo từng khoảng 30 phút
            var intervalRevenue = orders
                .Where(o => o.Status == "Đã giao")
                .GroupBy(o =>
                {
                    var time = o.CreatedAt;
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


            // Gán dữ liệu cho ViewBag
            ViewBag.TotalOrders = totalOrders;
            ViewBag.Completed = completed;
            ViewBag.Delivering = delivering;
            ViewBag.Cancelled = cancelled;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ProductStats = productStats;
            ViewBag.HourlyRevenue = intervalRevenue;

            return View("~/Views/Internal/Internal.cshtml", orders);
        }



        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;

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

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;

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


        // 🔹 Xem chi tiết đơn hàng (popup trong tab Đơn hàng đang tiến hành)
        [HttpGet]
        public async Task<IActionResult> OrderDetailsView(int id)
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;

            if (branchId == null)
                return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.ProductSize)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.BranchID == branchId);

            if (order == null)
                return NotFound();


            return PartialView("_OrderDetailsView", order);
        }

        [HttpPost]
        public async Task<IActionResult> DeliverOrder(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return Json(new { success = false, message = "❌ Không tìm thấy đơn hàng." });

            if (order.Status == "Đã xác nhận")
            {
                order.Status = "Đang giao";
                await _context.SaveChangesAsync();
                return Json(new { success = true, next = "Delivering", message = "Đơn hàng đã được thông báo cho shipper!" });
            }

            return Json(new { success = false, message = "Đơn hàng này không thể giao." });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null)
                return Json(new { success = false, message = "❌ Không tìm thấy đơn hàng." });

            if (order.Status == "Đang giao")
            {
                order.Status = "Đã giao";
                await _context.SaveChangesAsync();
                return Json(new { success = true, next = "Done", message = "Đơn hàng đã hoàn tất thành công!" });
            }

            return Json(new { success = true, message = "Đơn hàng đã được hoàn tất!" });
        }


        private string? GetCurrentShiftName(DateTime now)
        {
            var t = now.TimeOfDay;
            if (t < new TimeSpan(15, 0, 0)) return "Sáng";
            if (t < new TimeSpan(24, 0, 0)) return "Tối";
            return null;
        }

        public IActionResult GetEmployeesInCurrentShift()
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;

            if (branchId == null)
                return Unauthorized("Không xác định chi nhánh");

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

            ViewBag.CurrentShift = currentShift;
            return PartialView("EmployeesInShiftPartial", employees);
        }


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
                    ? $"❌ Đã ghi nhận phạt {Math.Abs(amount):N0}đ cho nhân viên {dto.EmployeeID}"
                    : $"✅ Đã thưởng {amount:N0}đ cho nhân viên {dto.EmployeeID}"
            });
        }


        [HttpGet]
        public async Task<IActionResult> ShiftReport()
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;
            var employeeId = HttpContext.Session.GetString("EmployeeID");

            if (branchId == null || string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            var work = await _context.WorkSchedules
                .FirstOrDefaultAsync(w => w.EmployeeID == employeeId && w.Date == today && w.IsActive);

            if (work == null)
            {
                ViewBag.Message = "❌ Không có lịch làm việc hôm nay.";
                return View("~/Views/Internal/ShiftReport.cshtml", new List<Order>());
            }

            var shift = work.Shift ?? "Morning";
            DateTime startTime, endTime;
            if (shift.Equals("Morning", StringComparison.OrdinalIgnoreCase))
            {
                startTime = today.AddHours(0);
                endTime = today.AddHours(14).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                startTime = today.AddHours(15);
                endTime = today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            // 📊 Lấy đơn trong ca
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o => o.BranchID == branchId && o.CreatedAt >= startTime && o.CreatedAt <= endTime)
                .ToListAsync();

            // ✅ Thống kê
            var totalOrders = orders.Count;
            var completed = orders.Count(o => o.Status == "Đã giao");
            var delivering = orders.Count(o => o.Status == "Đang giao");
            var cancelled = orders.Count(o => o.Status == "Đã hủy");
            var totalRevenue = orders
    .Where(o => o.Status == "Đã giao")
    .Select(o => o.Total)
    .DefaultIfEmpty(0m)
    .Sum();


            // 🔍 Thống kê chi tiết sản phẩm
            var productStats = orders
                .Where(o => o.Status == "Đã giao")
                .SelectMany(o => o.OrderDetails!)
                .GroupBy(od => od.Product!.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // 🕓 Biểu đồ doanh thu theo giờ
            var hourlyRevenue = orders
                .Where(o => o.Status == "Đã giao")
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderBy(x => x.Hour)
                .ToList();

            ViewBag.Shift = shift;
            ViewBag.Date = today.ToString("dd/MM/yyyy");
            ViewBag.TotalOrders = totalOrders;
            ViewBag.Completed = completed;
            ViewBag.Delivering = delivering;
            ViewBag.Cancelled = cancelled;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ProductStats = productStats;
            ViewBag.HourlyRevenue = hourlyRevenue;

            return View("~/Views/Internal/ShiftReport.cshtml", orders);
        }



        [HttpGet]
        public async Task<IActionResult> ExportRevenueToExcel()
        {
            var branchIdString = HttpContext.Session.GetString("BranchId");
            int? branchId = !string.IsNullOrEmpty(branchIdString) ? int.Parse(branchIdString) : (int?)null;
            var employeeId = HttpContext.Session.GetString("EmployeeID");
            string leaderName = HttpContext.Session.GetString("EmployeeName") ?? "Không xác định";
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchID == branchId);
            string branchName = branch?.Name ?? "Không rõ cơ sở";
            if (branchId == null || string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            // Lấy ca từ Session (đã lưu ở BranchOrders)
            var shift = HttpContext.Session.GetString("SelectedShift") ?? "Morning";
            DateTime startTime, endTime;
            if (shift.Equals("Morning", StringComparison.OrdinalIgnoreCase))
            {
                startTime = today.AddHours(0);
                endTime = today.AddHours(14).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                startTime = today.AddHours(15);
                endTime = today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            // Lấy đơn hàng trong khoảng thời gian ca làm
            var orders = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .Where(o => o.BranchID == branchId && o.CreatedAt >= startTime && o.CreatedAt <= endTime)
                .ToListAsync();

            // Tính thống kê
            var totalOrders = orders.Count;
            var completed = orders.Count(o => o.Status == "Đã giao");
            var delivering = orders.Count(o => o.Status == "Đang giao");
            var cancelled = orders.Count(o => o.Status == "Đã hủy");
            var totalRevenue = orders.Where(o => o.Status == "Đã giao").Sum(o => o.Total);

            // Thống kê chi tiết sản phẩm
            var productStats = orders
                .Where(o => o.Status == "Đã giao")
                .SelectMany(o => o.OrderDetails!)
                .GroupBy(od => od.Product!.ProductName)
                .Select(g => new
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // === Xuất Excel ===
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
            // Tổng quan
            ws.Cell("A7").Value = "Chỉ tiêu";
            ws.Cell("B7").Value = "Giá trị";
            ws.Range("A7:B7").Style.Font.Bold = true;
            ws.Range("A7:B7").Style.Fill.BackgroundColor = XLColor.LightGreen;



            var summary = new List<(string Label, object Value)>
    {
        ("Tổng đơn hàng", totalOrders),
        ("Đơn hoàn tất", completed),
        ("Đơn đang giao", delivering),
        ("Đơn hủy", cancelled),
        ("Tổng doanh thu (₫)", totalRevenue)
    };

            int row = 8;
            foreach (var s in summary)
            {
                ws.Cell(row, 1).Value = s.Label;

                // ép kiểu thủ công, nếu là số thì giữ nguyên, còn không thì convert sang chuỗi
                if (s.Value is int i)
                    ws.Cell(row, 2).Value = i;
                else if (s.Value is double d)
                    ws.Cell(row, 2).Value = d;
                else if (s.Value is decimal dec)
                    ws.Cell(row, 2).Value = dec;
                else
                    ws.Cell(row, 2).Value = s.Value?.ToString();

                row++;
            }



            // Dòng trống rồi bảng chi tiết
            row += 2;
            ws.Cell(row, 1).Value = "SẢN PHẨM BÁN RA";
            ws.Range(row, 1, row, 3).Merge().Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            ws.Cell(row, 1).Value = "Tên sản phẩm";
            ws.Cell(row, 2).Value = "Số lượng";
            ws.Cell(row, 3).Value = "Doanh thu (₫)";
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;
            row++;

            foreach (var p in productStats)
            {
                ws.Cell(row, 1).Value = p.ProductName;
                ws.Cell(row, 2).Value = p.Quantity;
                ws.Cell(row, 3).Value = p.Revenue;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string safeBranch = new string(branchName
      .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
      .ToArray());

            string fileName = $"BaoCao_DoanhThu_{safeBranch}_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> UploadShiftReport(IFormFile excelFile, IFormFile? imageFile)
        {
            // 🟢 Lấy branchId và shift từ form thay vì session
            var branchIdStr = Request.Form["branchId"].FirstOrDefault();
            var shift = Request.Form["shift"].FirstOrDefault();
            var today = DateTime.Today;

            if (string.IsNullOrEmpty(branchIdStr) || string.IsNullOrEmpty(shift))
                return BadRequest(new { success = false, message = "Không xác định được chi nhánh hoặc ca làm." });

            int branchId = int.Parse(branchIdStr);

            // Convert sang tiếng Việt cho đồng nhất DB
            string shiftVN = shift.Equals("Morning", StringComparison.OrdinalIgnoreCase) ? "Sáng" : "Tối";

            // Kiểm tra xem hôm nay, ca đó, chi nhánh đó đã nộp báo cáo chưa
            var existingReport = await _context.ShiftReports
                .FirstOrDefaultAsync(r => r.BranchID == branchId && r.Shift == shiftVN && r.Day == today);

            // Lưu file Excel
            string? excelPath = null;
            if (excelFile != null && excelFile.Length > 0)
            {
                var fileName = $"BaoCao_{shiftVN}_{today:ddMMyyyy}_{Path.GetFileName(excelFile.FileName)}";
                var filePath = Path.Combine("wwwroot/uploads/reports", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await excelFile.CopyToAsync(stream);
                }
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                excelPath = $"{baseUrl}/uploads/reports/{fileName}";
            }

            // Lưu ảnh (nếu có)
            string? imgPath = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var imgName = $"Chart_{shiftVN}_{today:ddMMyyyy}_{Path.GetFileName(imageFile.FileName)}";
                var imgFilePath = Path.Combine("wwwroot/uploads/reports", imgName);
                Directory.CreateDirectory(Path.GetDirectoryName(imgFilePath)!);
                using (var stream = new FileStream(imgFilePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                imgPath = $"/uploads/reports/{imgName}";
            }

            if (existingReport != null)
            {
                // Cập nhật nếu đã tồn tại
                if (excelPath != null) existingReport.Excel_Url = excelPath;
                if (imgPath != null) existingReport.Report_Img = imgPath;
                existingReport.LastUpdate = DateTime.Now;
            }
            else
            {
                // Tạo mới
                var report = new ShiftReport
                {
                    Excel_Url = excelPath,
                    Report_Img = imgPath,
                    LastUpdate = DateTime.Now,
                    Day = today,
                    Shift = shiftVN,
                    BranchID = branchId
                };
                _context.ShiftReports.Add(report);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "✅ Nộp báo cáo thành công!" });
        }



    }

}

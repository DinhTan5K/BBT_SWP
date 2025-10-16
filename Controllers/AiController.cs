using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Services;
using System.Linq;
using System.Threading.Tasks;
using Markdig;

namespace start.Controllers
{
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AiService _ai;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AiController(ApplicationDbContext db, AiService ai, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _ai = ai;
            _httpContextAccessor = httpContextAccessor; // Khởi tạo
        }

        // Trong AiController.cs

        // TRONG AiController.cs

        [HttpPost]
        public async Task<IActionResult> Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { answer = "Hãy nhập câu hỏi nhé!" });
            }

            try
            {
                // 0. LẤY CUSTOMER ID HIỆN TẠI TỪ SESSION
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                string customerContext = "";

                if (customerId.HasValue)
                {
                    // 0a. Lấy TÊN khách hàng (Tùy chọn)
                    var customer = await _db.Customers.FindAsync(customerId.Value);
                    string customerName = customer?.Name ?? "Khách hàng thân thiết";

                    // 0b. Lấy lịch sử đơn hàng gần nhất (chỉ lấy 3 đơn gần nhất để tiết kiệm token)
                    var orderHistory = await _db.Orders
                        .Where(o => o.CustomerID == customerId.Value)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(3)
                        .Select(o => new
                        {
                            o.OrderID,
                            o.CreatedAt,
                            o.Status,
                            // Lấy tên sản phẩm trong từng đơn hàng
                            Items = o.OrderDetails.Select(od => od.ProductSize.Product.ProductName + (od.Quantity > 1 ? $" x{od.Quantity}" : "")).ToList()
                        })
                        .ToListAsync();

                    if (orderHistory.Any())
                    {
                        // Xây dựng Context Lịch sử mua hàng
                        string historyDetail = string.Join("\n", orderHistory.Select(o =>
                        {
                            string items = string.Join(", ", o.Items);
                            return $"- Đơn hàng #{o.OrderID} (ngày {o.CreatedAt:dd/MM}): Trạng thái '{o.Status}'. Đã mua: {items}.";
                        }));

                        customerContext = $"Bạn đang nói chuyện với {customerName}. LỊCH SỬ MUA HÀNG GẦN NHẤT của họ:\n{historyDetail}";
                    }
                    else
                    {
                        customerContext = $"Bạn đang nói chuyện với {customerName}. Họ chưa có đơn hàng nào.";
                    }
                }
                else
                {
                    // Nếu khách hàng chưa đăng nhập
                    customerContext = "Khách hàng đang ở chế độ ẩn danh (chưa đăng nhập). Bạn không có lịch sử mua hàng của họ.";
                }
                // --- Kết thúc Context Khách hàng ---

                // 1. Lấy dữ liệu SẢN PHẨM và GIÁ
                var products = await _db.Products
                    // ... (Giữ nguyên phần này) ...
                    .Select(p => new
                    {
                        p.ProductName,
                        p.Description,
                        Sizes = p.ProductSizes.Select(s => new { s.Size, s.Price })
                    })
                    .Take(10)
                    .ToListAsync();

                // 2. Lấy dữ liệu CHI NHÁNH
                var branches = await _db.Branches
                    // ... (Giữ nguyên phần này) ...
                    .Select(b => new { b.Name, b.Address, b.Phone })
                    .Take(5)
                    .ToListAsync();

                // 3. Lấy dữ liệu KHUYẾN MÃI (Chỉ lấy các mã đang hoạt động)
                var now = DateTime.Now;
                var discounts = await _db.Discounts
                    // ... (Giữ nguyên phần này) ...
                    .Where(d => d.IsActive && (d.StartAt == null || d.StartAt <= now) && (d.EndAt == null || d.EndAt >= now))
                    .Select(d => new { d.Code, d.Percent, d.Amount, d.Type })
                    .Take(3)
                    .ToListAsync();


                // --- Xây dựng Context cho AI ---
                // Chi tiết sản phẩm (bao gồm giá)
                string productContext = string.Join("\n---\n", products.Select(p =>
                {
                    string sizeInfo = string.Join(", ", p.Sizes.Select(s => $"Size {s.Size}: {s.Price:N0} VND"));
                    return $"Sản phẩm: {p.ProductName}. Mô tả: {p.Description}. Chi tiết giá: {sizeInfo}.";
                }));

                // Chi tiết Chi nhánh
                string branchContext = string.Join("\n", branches.Select(b => $"Chi nhánh: {b.Name}. Địa chỉ: {b.Address}. SĐT: {b.Phone}."));

                // Chi tiết Khuyến mãi
                string discountContext = discounts.Count > 0
                    ? string.Join("\n", discounts.Select(d =>
                    {
                        string value = d.Type == DiscountType.Percentage ? $"{d.Percent}%" : $"{d.Amount:N0} VND";
                        return $"Mã: {d.Code}. Giảm: {value}. Loại: {d.Type}.";
                    }))
                    : "Hiện tại không có mã giảm giá đang hoạt động.";


                // Gộp tất cả dữ liệu vào System Instruction để AI có cái nhìn tổng quan
                string fullContext = $"DỮ LIỆU THAM KHẢO:\n\n*THÔNG TIN KHÁCH HÀNG:*\n{customerContext}\n\n*DANH SÁCH SẢN PHẨM (Kèm Giá):*\n{productContext}\n\n*CHI NHÁNH CỬA HÀNG:*\n{branchContext}\n\n*MÃ KHUYẾN MÃI ĐANG HOẠT ĐỘNG:*\n{discountContext}";

                string systemInstruction = "Bạn là nhân viên tư vấn chatbot thân thiện và chuyên nghiệp của quán trà sữa 'Buble Tea'. Dựa trên DỮ LIỆU THAM KHẢO (thông tin khách hàng, sản phẩm, chi nhánh, khuyến mãi), hãy trả lời câu hỏi của khách hàng. Hãy sử dụng LỊCH SỬ MUA HÀNG để gợi ý, thống kê hoặc tư vấn cá nhân hóa (ví dụ: 'Bạn đã mua X lần, thử món Y nhé!'). Luôn giữ giọng điệu thân mật (dùng 'bro', 'nhé', 'ơi').";

                string prompt = $"{systemInstruction}\n\n{fullContext}\n\nCâu hỏi của khách hàng: {question}";


                // Gọi AI service
                string aiResponse = await _ai.AskAIAsync(prompt);
                string htmlAnswer = Markdig.Markdown.ToHtml(aiResponse);
                // Loại bỏ thẻ <p> không cần thiết, nhưng giữ lại các định dạng Markdown khác
                htmlAnswer = htmlAnswer.Replace("<p>", "").Replace("</p>", "").Trim();

                return Json(new { answer = htmlAnswer });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết hơn nếu có thể
                // Console.WriteLine($"Lỗi trong AiController: {ex.Message}");
                return Json(new { answer = $"Có lỗi xảy ra: {ex.Message}. Vui lòng kiểm tra log server." });
            }
        }
    }
}

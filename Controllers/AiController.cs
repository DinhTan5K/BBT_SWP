using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Services;
using start.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Markdig;

namespace start.Controllers
{
    public class AiController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AiService _ai;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;

        // Constructor DUY NHẤT
        public AiController(ApplicationDbContext db, AiService ai, IHttpContextAccessor httpContextAccessor, ICartService cartService, IOrderService orderService)
        {
            _db = db;
            _ai = ai;
            _httpContextAccessor = httpContextAccessor;
            _cartService = cartService;
            _orderService = orderService;
        }

        // Hàm helper: Lưu lịch sử chat vào database
        private async Task SaveChatHistoryAsync(int? customerId, string question, string answer)
        {
            try
            {
                // Kiểm tra customerId hợp lệ
                if (customerId == null)
                {
                    Console.WriteLine("⚠️ Không thể lưu chat history: CustomerID is null (user chưa đăng nhập)");
                    return;
                }

                var chatHistory = new ChatHistory
                {
                    CustomerID = customerId,
                    Question = !string.IsNullOrEmpty(question) && question.Length > 1000 ? question.Substring(0, 1000) : (question ?? ""), // Giới hạn độ dài
                    Answer = answer ?? "",
                    CreatedAt = DateTime.Now
                };
                
                _db.ChatHistories.Add(chatHistory);
                int saved = await _db.SaveChangesAsync();
                
                if (saved > 0)
                {
                    Console.WriteLine($"✅ Đã lưu chat history thành công cho CustomerID: {customerId}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Không có thay đổi nào được lưu vào database");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết để debug
                Console.WriteLine($"❌ LỖI lưu chat history:");
                Console.WriteLine($"   CustomerID: {customerId}");
                string questionPreview = string.IsNullOrEmpty(question) ? "" : (question.Length > 50 ? question.Substring(0, 50) : question);
                Console.WriteLine($"   Question: {questionPreview}...");
                Console.WriteLine($"   Error: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                
                // Nếu là lỗi table không tồn tại, in ra hướng dẫn
                if (ex.Message.Contains("ChatHistory") || ex.Message.Contains("Invalid object name"))
                {
                    Console.WriteLine($"\n⚠️ CẢNH BÁO: Table ChatHistory chưa được tạo trong database!");
                    Console.WriteLine($"   Hãy chạy script SQL: Scripts/CreateChatHistoryTable.sql");
                }
            }
        }

        // Hàm helper: Chuyển tiếng Việt có dấu thành không dấu
        private string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            string[] vietnameseChars = { "à", "á", "ạ", "ả", "ã", "â", "ầ", "ấ", "ậ", "ẩ", "ẫ", "ă", "ằ", "ắ", "ặ", "ẳ", "ẵ",
                "è", "é", "ẹ", "ẻ", "ẽ", "ê", "ề", "ế", "ệ", "ể", "ễ",
                "ì", "í", "ị", "ỉ", "ĩ",
                "ò", "ó", "ọ", "ỏ", "õ", "ô", "ồ", "ố", "ộ", "ổ", "ỗ", "ơ", "ờ", "ớ", "ợ", "ở", "ỡ",
                "ù", "ú", "ụ", "ủ", "ũ", "ư", "ừ", "ứ", "ự", "ử", "ữ",
                "ỳ", "ý", "ỵ", "ỷ", "ỹ",
                "đ" };
            
            string[] replaceChars = { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e",
                "i", "i", "i", "i", "i",
                "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u",
                "y", "y", "y", "y", "y",
                "d" };

            string result = text.ToLower();
            for (int i = 0; i < vietnameseChars.Length; i++)
            {
                result = result.Replace(vietnameseChars[i], replaceChars[i]);
                result = result.Replace(vietnameseChars[i].ToUpper(), replaceChars[i].ToUpper());
            }
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { answer = "Hãy nhập câu hỏi nhé!" });
            }

            string lower = question.ToLower();
            string lowerNoAccent = RemoveVietnameseAccents(question.ToLower());
            
            // --- TOP 1: XEM GIỎ HÀNG HIỆN TẠI ---
            if (lower.Contains("giỏ hàng") || lower.Contains("gio hang") || lower.Contains("cart") ||
                lower.Contains("xem giỏ") || lower.Contains("xem gio") || lower.Contains("giỏ của tôi") || 
                lower.Contains("gio cua toi") || lower.Contains("có gì trong giỏ") || lower.Contains("co gi trong gio"))
            {
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { answer = "Bạn cần đăng nhập để xem giỏ hàng nhé! 🔐", redirectUrl = Url.Action("Login", "Account") });
                }

                var cartItems = _cartService.GetCartItems(customerId.Value);
                
                if (cartItems == null || cartItems.Count == 0)
                {
                    string answer = "Giỏ hàng của bạn đang trống! Bạn muốn đặt món gì không? 🛒";
                    await SaveChatHistoryAsync(customerId, question, answer);
                    return Json(new { answer = answer });
                }

                decimal totalAmount = 0;
                var itemsList = new List<string>();
                
                foreach (var item in cartItems)
                {
                    dynamic itemObj = item;
                    string productName = itemObj.ProductName?.ToString() ?? "Unknown";
                    string size = itemObj.Size?.ToString() ?? "N/A";
                    int quantity = (int)(itemObj.Quantity ?? 0);
                    decimal total = (decimal)(itemObj.Total ?? 0);
                    
                    totalAmount += total;
                    itemsList.Add($"• {productName} size {size} (x{quantity}) - {total:N0} đ");
                }

                string cartSummary = $"**Giỏ hàng của bạn có {cartItems.Count} món:**\n\n" +
                    string.Join("\n", itemsList) +
                    $"\n\n💰 **Tổng tiền: {totalAmount:N0} đ**\n\n" +
                    "Bạn muốn thêm món gì không? Hoặc muốn xóa món nào ra? 😊";

                await SaveChatHistoryAsync(customerId, question, cartSummary);
                return Json(new { answer = cartSummary });
            }

            // --- TOP 2: THỐNG KÊ CÁ NHÂN ---
            if (lower.Contains("thống kê") || lower.Contains("thong ke") || lower.Contains("đã mua") || 
                lower.Contains("da mua") || lower.Contains("mua bao nhiêu") || lower.Contains("mua bao nhieu") ||
                lower.Contains("tổng tiền đã chi") || lower.Contains("tong tien da chi") || lower.Contains("chi tiêu") ||
                lower.Contains("chi tieu") || lower.Contains("món nào mua nhiều") || lower.Contains("mon nao mua nhieu"))
            {
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { answer = "Bạn cần đăng nhập để xem thống kê nhé! 🔐", redirectUrl = Url.Action("Login", "Account") });
                }

                var orders = await _db.Orders
                    .Where(o => o.CustomerID == customerId.Value)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .ToListAsync();

                if (!orders.Any())
                {
                    string answer = "Bạn chưa có đơn hàng nào cả! Hãy đặt món để bắt đầu nhé! 🛒";
                    await SaveChatHistoryAsync(customerId, question, answer);
                    return Json(new { answer = answer });
                }

                // Thống kê
                int totalOrders = orders.Count;
                decimal totalSpent = orders.Sum(o => o.Total);
                
                // Đếm món nào mua nhiều nhất
                var productCounts = orders
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(od => od.Product?.ProductName ?? "Unknown")
                    .Select(g => new { ProductName = g.Key, Count = g.Sum(od => od.Quantity) })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();

                string topProducts = string.Join("\n", productCounts.Select((p, i) => $"{i + 1}. {p.ProductName} ({p.Count} lần)"));

                string stats = $"📊 **Thống kê cá nhân của bạn:**\n\n" +
                    $"🛒 Tổng số đơn hàng: **{totalOrders}** đơn\n" +
                    $"💰 Tổng tiền đã chi: **{totalSpent:N0} đ**\n" +
                    $"📈 Đơn hàng trung bình: **{(totalSpent / totalOrders):N0} đ**\n\n" +
                    $"🏆 **Top 5 món bạn mua nhiều nhất:**\n{topProducts}\n\n" +
                    "Bạn có muốn đặt lại món nào không? 😊";

                await SaveChatHistoryAsync(customerId, question, stats);
                return Json(new { answer = stats });
            }

            // --- TOP 3: TRA CỨU ĐƠN HÀNG ---
            if (lower.Contains("đơn hàng") || lower.Contains("don hang") || lower.Contains("order") ||
                lower.Contains("kiểm tra đơn") || lower.Contains("kiem tra don") || lower.Contains("tra cứu") ||
                lower.Contains("tra cuu") || lower.Contains("#") || Regex.IsMatch(question, @"#\d+"))
            {
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { answer = "Bạn cần đăng nhập để tra cứu đơn hàng nhé! 🔐", redirectUrl = Url.Action("Login", "Account") });
                }

                // Tìm số đơn hàng trong câu hỏi
                var orderNumberMatch = Regex.Match(question, @"#?(\d+)");
                int? orderId = null;
                
                if (orderNumberMatch.Success)
                {
                    orderId = int.Parse(orderNumberMatch.Groups[1].Value);
                }

                if (!orderId.HasValue)
                {
                    // Không có số đơn, liệt kê 5 đơn gần nhất
                    var recentOrders = await _db.Orders
                        .Where(o => o.CustomerID == customerId.Value)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(5)
                        .Select(o => new { o.OrderID, o.OrderCode, o.Status, o.CreatedAt, o.Total })
                        .ToListAsync();

                    if (!recentOrders.Any())
                    {
                        string answer = "Bạn chưa có đơn hàng nào!";
                        await SaveChatHistoryAsync(customerId, question, answer);
                        return Json(new { answer = answer });
                    }

                    string ordersList = "📦 **5 đơn hàng gần nhất của bạn:**\n\n";
                    foreach (var orderItem in recentOrders)
                    {
                        ordersList += $"• Đơn #{orderItem.OrderID} ({orderItem.OrderCode}) - {orderItem.Status}\n";
                        ordersList += $"  Ngày: {orderItem.CreatedAt:dd/MM/yyyy} - Tổng: {orderItem.Total:N0} đ\n\n";
                    }
                    ordersList += "Bạn muốn xem chi tiết đơn nào? Nhập 'Đơn hàng #X' hoặc 'Kiểm tra đơn X' nhé! 😊";

                    await SaveChatHistoryAsync(customerId, question, ordersList);
                    return Json(new { answer = ordersList });
                }

                // Có số đơn hàng, lấy chi tiết
                var order = await _orderService.GetOrderByIdAsync(orderId.Value);
                
                if (order == null || order.CustomerID != customerId.Value)
                {
                    string answer = $"Không tìm thấy đơn hàng #{orderId.Value} hoặc đơn hàng không thuộc về bạn!";
                    await SaveChatHistoryAsync(customerId, question, answer);
                    return Json(new { answer = answer });
                }

                var orderDetails = await _db.OrderDetails
                    .Where(od => od.OrderID == order.OrderID)
                    .Include(od => od.Product)
                    .Include(od => od.ProductSize)
                    .ToListAsync();

                string itemsList = string.Join("\n", orderDetails.Select(od => 
                    $"• {od.Product?.ProductName} size {od.ProductSize?.Size} (x{od.Quantity}) - {od.Total:N0} đ"));

                string orderInfo = $"📦 **Chi tiết đơn hàng #{order.OrderID} ({order.OrderCode}):**\n\n" +
                    $"📅 Ngày đặt: {order.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                    $"📊 Trạng thái: **{order.Status}**\n" +
                    $"💰 Tổng tiền: **{order.Total:N0} đ**\n" +
                    $"🚚 Phí ship: {order.ShippingFee:N0} đ\n" +
                    $"📍 Địa chỉ: {order.Address} {order.DetailAddress}\n" +
                    $"📞 SĐT: {order.ReceiverPhone}\n\n" +
                    $"**Các món đã đặt:**\n{itemsList}";

                if (!string.IsNullOrEmpty(order.NoteOrder))
                {
                    orderInfo += $"\n\n💬 Ghi chú: {order.NoteOrder}";
                }

                await SaveChatHistoryAsync(customerId, question, orderInfo);
                return Json(new { answer = orderInfo });
            }

            // --- Nhận diện intent hỏi về MENU/MÓN NGON (ĐẶT TRƯỚC LOGIC XÓA) ---
            // Chỉ xử lý nếu KHÔNG có từ khóa hành động (xóa, đặt, thêm, mua)
            bool hasActionKeyword = lower.Contains("xóa") || lowerNoAccent.Contains("xoa") ||
                                   lower.Contains("bỏ") || lowerNoAccent.Contains("bo") ||
                                   lower.Contains("gỡ") || lowerNoAccent.Contains("go") ||
                                   lower.Contains("đặt") || lowerNoAccent.Contains("dat") ||
                                   lower.Contains("thêm") || lowerNoAccent.Contains("them") ||
                                   lower.Contains("mua") || lowerNoAccent.Contains("mua") ||
                                   lower.Contains("remove") || lower.Contains("delete") ||
                                   lower.Contains("add") || lower.Contains("order");

            // Mở rộng điều kiện nhận diện câu hỏi về menu
            bool isMenuQuestion = !hasActionKeyword && (
                // Các từ khóa chính
                lower.Contains("món") || lowerNoAccent.Contains("mon") ||
                lower.Contains("ngon") || lowerNoAccent.Contains("ngon") ||
                lower.Contains("menu") || lowerNoAccent.Contains("menu") ||
                // Các câu hỏi phổ biến
                lower.Contains("có gì") || lowerNoAccent.Contains("co gi") ||
                lower.Contains("có món") || lowerNoAccent.Contains("co mon") ||
                lower.Contains("mon gi") || lowerNoAccent.Contains("mon gi") ||
                lower.Contains("mon nao") || lowerNoAccent.Contains("mon nao") ||
                lower.Contains("mon vay") || lowerNoAccent.Contains("mon vay") ||
                lower.Contains("mon ngon") || lowerNoAccent.Contains("mon ngon") ||
                lower.Contains("mon nao ngon") || lowerNoAccent.Contains("mon nao ngon") ||
                // Từ khóa khác
                lower.Contains("giới thiệu") || lowerNoAccent.Contains("gioi thieu") ||
                lower.Contains("sản phẩm") || lowerNoAccent.Contains("san pham") ||
                lower.Contains("đồ uống") || lowerNoAccent.Contains("do uong") ||
                lower.Contains("gợi ý") || lowerNoAccent.Contains("goi y") ||
                lower.Contains("nên uống") || lowerNoAccent.Contains("nen uong") ||
                lower.Contains("nên mua") || lowerNoAccent.Contains("nen mua") ||
                lower.Contains("trà sữa") || lowerNoAccent.Contains("tra sua") ||
                lower.Contains("ban co") || lowerNoAccent.Contains("ban co"));

            if (isMenuQuestion)
            {
                // Lấy danh sách sản phẩm
                var allProducts = await _db.Products
                    .Include(p => p.ProductSizes)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .Take(15) // Lấy 15 món đầu tiên
                    .ToListAsync();

                if (!allProducts.Any())
                {
                    return Json(new { answer = "Hiện tại cửa hàng chưa có sản phẩm nào bro! Vui lòng quay lại sau nhé! 😊" });
                }

                // Lấy customer ID để có thể tư vấn cá nhân hóa
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                string personalizedContext = "";

                if (customerId.HasValue)
                {
                    // Lấy lịch sử mua hàng để gợi ý
                    var recentOrders = await _db.Orders
                        .Where(o => o.CustomerID == customerId.Value)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(5)
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                        .ToListAsync();

                    var popularProducts = recentOrders
                        .SelectMany(o => o.OrderDetails)
                        .GroupBy(od => od.Product?.ProductName ?? "")
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .OrderByDescending(g => g.Sum(od => od.Quantity))
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList();

                    if (popularProducts.Any())
                    {
                        personalizedContext = $"\n\n💡 Dựa vào lịch sử mua hàng, bạn thường uống: {string.Join(", ", popularProducts)}. Bạn có muốn đặt lại những món này không?";
                    }
                }

                // Xây dựng danh sách món với giá
                var productList = new List<string>();
                foreach (var product in allProducts)
                {
                    var minPrice = product.ProductSizes?.Any() == true 
                        ? product.ProductSizes.Min(ps => ps.Price) 
                        : 0;
                    var maxPrice = product.ProductSizes?.Any() == true 
                        ? product.ProductSizes.Max(ps => ps.Price) 
                        : 0;
                    
                    string priceRange = "";
                    if (minPrice > 0 && maxPrice > 0)
                    {
                        if (minPrice == maxPrice)
                            priceRange = $" - {minPrice:N0} đ";
                        else
                            priceRange = $" - {minPrice:N0} đ - {maxPrice:N0} đ";
                    }
                    
                    string description = !string.IsNullOrEmpty(product.Description) 
                        ? $" ({product.Description})" 
                        : "";
                    
                    productList.Add($"• **{product.ProductName}**{priceRange}{description}");
                }

                string answer = $"🍵 **Danh sách món ngon của Buble Tea:**\n\n" +
                    string.Join("\n", productList) +
                    $"\n\nBạn muốn đặt món nào không? Chỉ cần nói 'đặt [tên món] size [S/M/L]' là được nhé! 😊{personalizedContext}";

                await SaveChatHistoryAsync(customerId, question, answer);
                return Json(new { answer = answer });
            }

            // --- Nhận diện intent XÓA MÓN khỏi giỏ hàng ---
            // CHỈ xử lý khi có từ khóa xóa/bỏ/gỡ/remove/delete RÕ RÀNG
            bool hasDeleteKeyword = lower.Contains("xóa") || lowerNoAccent.Contains("xoa") ||
                                   lower.Contains("bỏ") || lowerNoAccent.Contains("bo") ||
                                   lower.Contains("gỡ") || lowerNoAccent.Contains("go") ||
                                   lower.Contains("remove") || lower.Contains("delete");
            
            if (hasDeleteKeyword)
            {
                // Kiểm tra đăng nhập trước
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { answer = "Bạn cần đăng nhập để xóa món nhé! 🔐", redirectUrl = Url.Action("Login", "Account") });
                }

                // Lấy giỏ hàng hiện tại
                var cart = await _db.Carts
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Product)
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.ProductSize)
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

                if (cart == null || !cart.CartDetails.Any())
                {
                    return Json(new { answer = "Giỏ hàng của bạn đang trống, không có gì để xóa! 🛒" });
                }

                // Tìm sản phẩm trong giỏ hàng dựa trên tên món
                var allProducts = await _db.Products
                    .Where(p => p.IsActive)
                    .ToListAsync();

                Product? matchedProduct = null;
                foreach (var product in allProducts)
                {
                    string productNameLower = (product.ProductName ?? "").ToLower();
                    string productNameNoAccent = RemoveVietnameseAccents(productNameLower);
                    
                    if (lower.Contains(productNameLower) || lowerNoAccent.Contains(productNameNoAccent) ||
                        productNameLower.Split(' ', '-', '_', '/')
                            .Any(word => word.Length > 3 && (lower.Contains(word) || lowerNoAccent.Contains(RemoveVietnameseAccents(word)))))
                    {
                        matchedProduct = product;
                        break;
                    }
                }

                if (matchedProduct == null)
                {
                    // Liệt kê các món trong giỏ hàng để user chọn
                    var cartItems = cart.CartDetails.Select(cd => $"- {cd.Product?.ProductName} size {cd.ProductSize?.Size} (x{cd.Quantity})").ToList();
                    return Json(new { 
                        answer = $"Không tìm thấy món bạn muốn xóa. Các món trong giỏ hàng của bạn:\n\n" +
                        string.Join("\n", cartItems) +
                        "\n\nHãy nhập tên món cụ thể để xóa nhé! 💡"
                    });
                }

                // Kiểm tra xem có yêu cầu xóa "tất cả" không
                bool removeAll = lower.Contains("tất cả") || lower.Contains("tat ca") || 
                                lower.Contains("all") || lowerNoAccent.Contains("tat ca");

                // Tìm tất cả CartDetail có ProductID này
                var cartDetails = cart.CartDetails
                    .Where(cd => cd.ProductID == matchedProduct.ProductID)
                    .ToList();

                if (!cartDetails.Any())
                {
                    return Json(new { answer = $"Món {matchedProduct.ProductName} không có trong giỏ hàng của bạn! 🛒" });
                }

                string answerMessage = "";
                int deletedCount = 0;

                if (removeAll)
                {
                    // Xóa TẤT CẢ món matcha (kể cả khác size)
                    foreach (var cartDetail in cartDetails)
                    {
                        var updateReq = new UpdateCartRequest
                        {
                            CartDetailId = cartDetail.CartDetailID,
                            Quantity = 0
                        };

                        if (_cartService.UpdateCart(customerId.Value, updateReq, out string updateMsg))
                        {
                            deletedCount++;
                        }
                    }

                    if (deletedCount > 0)
                    {
                        answerMessage = $"✅ Đã xóa tất cả {matchedProduct.ProductName} ({deletedCount} món) khỏi giỏ hàng thành công! 🗑️";
                    }
                    else
                    {
                        answerMessage = $"❌ Không thể xóa các món {matchedProduct.ProductName}.";
                    }
                }
                else
                {
                    // Chỉ xóa 1 món đầu tiên
                    var cartDetail = cartDetails.FirstOrDefault();
                    var updateReq = new UpdateCartRequest
                    {
                        CartDetailId = cartDetail.CartDetailID,
                        Quantity = 0
                    };

                    if (_cartService.UpdateCart(customerId.Value, updateReq, out string updateMsg))
                    {
                        answerMessage = $"✅ Đã xóa {matchedProduct.ProductName} khỏi giỏ hàng thành công! 🗑️";
                        deletedCount = 1;
                    }
                    else
                    {
                        answerMessage = $"❌ Không thể xóa món: {updateMsg}";
                    }
                }

                // Lưu lịch sử chat
                await SaveChatHistoryAsync(customerId, question, answerMessage);
                
                // Nếu xóa thành công thì redirect về trang Product để refresh giỏ hàng
                if (deletedCount > 0)
                {
                    return Json(new 
                    { 
                        answer = answerMessage,
                        redirectUrl = Url.Action("Product", "Product")
                    });
                }
                
                return Json(new { answer = answerMessage });
            }

            // --- Nhận diện intent đặt món/thêm giỏ hàng ---
            if (lower.Contains("đặt") || lower.Contains("thêm") || lower.Contains("mua") || 
                lowerNoAccent.Contains("dat") || lowerNoAccent.Contains("them") || lowerNoAccent.Contains("mua"))
            {
                // Kiểm tra đăng nhập trước
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                if (customerId == null)
                {
                    return Json(new { answer = "Bạn cần đăng nhập để đặt món nhé! 🔐", redirectUrl = Url.Action("Login", "Account") });
                }

                // Lấy tất cả sản phẩm từ DB
                var allProducts = await _db.Products
                    .Include(p => p.ProductSizes)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                if (!allProducts.Any())
                {
                    return Json(new { answer = "Hiện tại không có sản phẩm nào trong cửa hàng. Vui lòng thử lại sau!" });
                }

                // Danh sách từ khóa quá chung - không nên tự động match
                var genericKeywords = new[] { "trà sữa", "tra sua", "trà", "tra", "sữa", "sua", "món", "mon", "đồ uống", "do uong", "đặt", "dat", "thêm", "them", "mua" };
                
                // Tìm sản phẩm phù hợp dựa trên từ khóa trong câu hỏi (hỗ trợ cả có dấu và không dấu)
                Product? matchedProduct = null;
                List<Product> matchedProducts = new List<Product>(); // Lưu tất cả sản phẩm match
                
                foreach (var product in allProducts)
                {
                    string productNameLower = (product.ProductName ?? "").ToLower();
                    string productNameNoAccent = RemoveVietnameseAccents(productNameLower);
                    
                    // Kiểm tra match chính xác tên sản phẩm (tên đầy đủ)
                    bool exactMatch = lower.Contains(productNameLower) || lowerNoAccent.Contains(productNameNoAccent);
                    
                    // Kiểm tra match từng từ trong tên sản phẩm (bỏ qua các từ chung chung)
                    var productWords = productNameLower.Split(' ', '-', '_', '/')
                        .Where(word => word.Length > 2 && !genericKeywords.Contains(word))
                        .ToList();
                    
                    bool specificMatch = productWords.Any(word => 
                        (lower.Contains(word) || lowerNoAccent.Contains(RemoveVietnameseAccents(word))) &&
                        word.Length > 3); // Chỉ match từ có độ dài > 3 ký tự để tránh match nhầm
                    
                    if (exactMatch || specificMatch)
                    {
                        matchedProducts.Add(product);
                        // Nếu match chính xác tên đầy đủ, ưu tiên sản phẩm đó
                        if (exactMatch)
                        {
                            matchedProduct = product;
                            break;
                        }
                    }
                }

                // Nếu có nhiều sản phẩm match hoặc chỉ match từ khóa chung, hỏi lại user
                if (matchedProduct == null)
                {
                    if (matchedProducts.Count == 0)
                    {
                        // Không tìm thấy sản phẩm nào, liệt kê một số sản phẩm phổ biến
                        var popularProducts = allProducts.Take(5).Select(p => p.ProductName).ToList();
                        return Json(new { 
                            answer = $"Không tìm thấy sản phẩm phù hợp. Bạn có muốn đặt một trong các món sau không?\n\n" +
                            string.Join("\n", popularProducts.Select((p, i) => $"{i + 1}. {p}")) +
                            "\n\nHãy nhập lại tên món cụ thể nhé! 💡" 
                        });
                    }
                    else if (matchedProducts.Count > 1)
                    {
                        // Có nhiều sản phẩm match, liệt kê cho user chọn
                        return Json(new { 
                            answer = $"Tìm thấy {matchedProducts.Count} sản phẩm phù hợp:\n\n" +
                            string.Join("\n", matchedProducts.Select((p, i) => $"{i + 1}. {p.ProductName}")) +
                            "\n\nHãy nhập lại tên món cụ thể hơn nhé! 💡" 
                        });
                    }
                    else
                    {
                        // Chỉ có 1 sản phẩm match, dùng nó
                        matchedProduct = matchedProducts.First();
                    }
                }

                // Phân tích size từ câu hỏi (hỗ trợ cả có dấu và không dấu)
                string sizeKeyword = "l"; // default
                if (lower.Contains("size l") || lowerNoAccent.Contains("size l") || 
                    (lower.Contains(" l") && !lower.Contains("size m") && !lower.Contains("size s")) ||
                    (lowerNoAccent.Contains(" l") && !lowerNoAccent.Contains("size m") && !lowerNoAccent.Contains("size s")))
                    sizeKeyword = "l";
                else if (lower.Contains("size m") || lowerNoAccent.Contains("size m") ||
                    (lower.Contains(" m") && !lower.Contains("size l") && !lower.Contains("size s")) ||
                    (lowerNoAccent.Contains(" m") && !lowerNoAccent.Contains("size l") && !lowerNoAccent.Contains("size s")))
                    sizeKeyword = "m";
                else if (lower.Contains("size s") || lowerNoAccent.Contains("size s") ||
                    (lower.Contains(" s") && !lower.Contains("size l") && !lower.Contains("size m")) ||
                    (lowerNoAccent.Contains(" s") && !lowerNoAccent.Contains("size l") && !lowerNoAccent.Contains("size m")))
                    sizeKeyword = "s";

                // Phân tích số lượng (hỗ trợ cả có dấu và không dấu)
                int quantity = 1;
                for (int i = 2; i <= 10; i++)
                {
                    if (lower.Contains($"{i} ly") || lower.Contains($"{i} suất") || lower.Contains($"{i} cốc") || lower.Contains($"{i} cái") ||
                        lowerNoAccent.Contains($"{i} ly") || lowerNoAccent.Contains($"{i} suat") || lowerNoAccent.Contains($"{i} coc") || lowerNoAccent.Contains($"{i} cai"))
                    {
                        quantity = i;
                        break;
                    }
                }

                // Tìm size phù hợp
                var matchedSize = matchedProduct.ProductSizes.FirstOrDefault(s => s.Size.ToLower().Contains(sizeKeyword));
                if (matchedSize == null)
                {
                    // Fallback: lấy size đầu tiên
                    matchedSize = matchedProduct.ProductSizes.FirstOrDefault();
                    if (matchedSize == null)
                    {
                        return Json(new { answer = $"Sản phẩm {matchedProduct.ProductName} hiện không có size nào. Vui lòng chọn sản phẩm khác!" });
                    }
                    sizeKeyword = matchedSize.Size.ToLower();
                }

                // Thêm vào giỏ hàng
                var addReq = new AddToCartRequest
                {
                    ProductId = matchedProduct.ProductID,
                    ProductSizeId = matchedSize.ProductSizeID,
                    Quantity = quantity,
                    Price = matchedSize.Price
                };

                string answerMessage = "";
                if (_cartService.AddToCart(customerId.Value, addReq, out string addMsg))
                {
                    answerMessage = $"✅ Đã thêm {quantity} {matchedProduct.ProductName} size {matchedSize.Size.ToUpper()} vào giỏ hàng! Đang chuyển sang trang mua hàng để bạn kiểm tra 🛒";
                    
                    // Lưu lịch sử chat TRƯỚC khi response (đảm bảo đã lưu xong)
                    await SaveChatHistoryAsync(customerId, question, answerMessage);
                    
                    // Thêm delay nhỏ để đảm bảo DB transaction commit xong
                    await Task.Delay(100); // 100ms delay
                    
                    return Json(new
                    {
                        answer = answerMessage,
                        redirectUrl = Url.Action("Product", "Product")
                    });
                }
                else
                {
                    answerMessage = $"❌ Không thể thêm vào giỏ: {addMsg}";
                    
                    // Lưu lịch sử chat
                    await SaveChatHistoryAsync(customerId, question, answerMessage);
                    
                    return Json(new { answer = answerMessage });
                }
            }

            // --- Xử lý AI cũ như mặc định ---
            try
            {
                // LẤY CUSTOMER ID HIỆN TẠI TỪ SESSION
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                string customerContext = "";

                if (customerId.HasValue)
                {
                    var customer = await _db.Customers.FindAsync(customerId.Value);
                    string customerName = customer?.Name ?? "Khách hàng thân thiết";

                    // Lịch sử đơn hàng gần nhất
                    var orderHistory = await _db.Orders
                        .Where(o => o.CustomerID == customerId.Value)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(3)
                        .Select(o => new
                        {
                            o.OrderID,
                            o.CreatedAt,
                            o.Status,
                            Items = o.OrderDetails.Select(od => od.ProductSize.Product.ProductName + (od.Quantity > 1 ? $" x{od.Quantity}" : "")).ToList()
                        })
                        .ToListAsync();

                    if (orderHistory.Any())
                    {
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
                    customerContext = "Khách hàng đang ở chế độ ẩn danh (chưa đăng nhập). Bạn không có lịch sử mua hàng của họ.";
                }
                // --- Kết thúc Context Khách hàng ---

                // Lấy dữ liệu sản phẩm và giá
                var products = await _db.Products
                    .Select(p => new
                    {
                        p.ProductName,
                        p.Description,
                        Sizes = p.ProductSizes.Select(s => new { s.Size, s.Price })
                    })
                    .Take(10)
                    .ToListAsync();

                // Lấy chi nhánh
                var branches = await _db.Branches
                    .Select(b => new { b.Name, b.Address, b.Phone })
                    .Take(5)
                    .ToListAsync();

                // Lấy mã khuyến mãi
                var now = DateTime.Now;
                var discounts = await _db.Discounts
                    .Where(d => d.IsActive && (d.StartAt == null || d.StartAt <= now) && (d.EndAt == null || d.EndAt >= now))
                    .Select(d => new { d.Code, d.Percent, d.Amount, d.Type })
                    .Take(3)
                    .ToListAsync();

                // Xây dựng context cho AI
                string productContext = string.Join("\n---\n", products.Select(p =>
                {
                    string sizeInfo = string.Join(", ", p.Sizes.Select(s => $"Size {s.Size}: {s.Price:N0} VND"));
                    return $"Sản phẩm: {p.ProductName}. Mô tả: {p.Description}. Chi tiết giá: {sizeInfo}.";
                }));

                string branchContext = string.Join("\n", branches.Select(b => $"Chi nhánh: {b.Name}. Địa chỉ: {b.Address}. SĐT: {b.Phone}."));
                string discountContext = discounts.Count > 0
                    ? string.Join("\n", discounts.Select(d =>
                    {
                        string value = d.Type == 0 ? $"{d.Percent}%" : $"{d.Amount:N0} VND";
                        return $"Mã: {d.Code}. Giảm: {value}. Loại: {d.Type}.";
                    }))
                    : "Hiện tại không có mã giảm giá đang hoạt động.";

                string fullContext = $"DỮ LIỆU THAM KHẢO:\n\n*THÔNG TIN KHÁCH HÀNG:*\n{customerContext}\n\n*DANH SÁCH SẢN PHẨM (Kèm Giá):*\n{productContext}\n\n*CHI NHÁNH CỬA HÀNG:*\n{branchContext}\n\n*MÃ KHUYẾN MÃI ĐANG HOẠT ĐỘNG:*\n{discountContext}";

                string systemInstruction = "Bạn là nhân viên tư vấn chatbot thân thiện và chuyên nghiệp của quán trà sữa 'Buble Tea'. Dựa trên DỮ LIỆU THAM KHẢO (thông tin khách hàng, sản phẩm, chi nhánh, khuyến mãi), hãy trả lời câu hỏi của khách hàng. Hãy sử dụng LỊCH SỬ MUA HÀNG để gợi ý, thống kê hoặc tư vấn cá nhân hóa (ví dụ: 'Bạn đã mua X lần, thử món Y nhé!'). Luôn giữ giọng điệu thân mật (dùng 'bro', 'nhé', 'ơi').";

                string prompt = $"{systemInstruction}\n\n{fullContext}\n\nCâu hỏi của khách hàng: {question}";

                // Gọi AI service
                string aiResponse = await _ai.AskAIAsync(prompt);
                string htmlAnswer = Markdig.Markdown.ToHtml(aiResponse);
                htmlAnswer = htmlAnswer.Replace("<p>", "").Replace("</p>", "").Trim();

                // Lưu lịch sử chat
                await SaveChatHistoryAsync(customerId, question, htmlAnswer);

                return Json(new { answer = htmlAnswer });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Có lỗi xảy ra: {ex.Message}. Vui lòng kiểm tra log server.";
                
                // Vẫn lưu lỗi vào lịch sử để user biết
                var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
                await SaveChatHistoryAsync(customerId, question, errorMessage);
                
                return Json(new { answer = errorMessage });
            }
        }

        // Endpoint để load lịch sử chat
        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var customerId = _httpContextAccessor.HttpContext?.Session.GetInt32("CustomerID");
            
            if (!customerId.HasValue)
            {
                return Json(new { history = new List<object>() });
            }
            
            // Lấy tất cả lịch sử, filter ở client-side để tránh vấn đề với EF
            var chatHistory = await _db.ChatHistories
                .Where(ch => ch.CustomerID == customerId)
                .OrderBy(ch => ch.CreatedAt)
                .Take(50) // Lấy 50 tin nhắn gần nhất
                .Select(ch => new
                {
                    question = ch.Question ?? "",  // Dùng camelCase cho JavaScript
                    answer = ch.Answer ?? "",      // Dùng camelCase cho JavaScript
                    createdAt = ch.CreatedAt
                })
                .ToListAsync();

            // Filter bỏ các record không hợp lệ (sau khi query từ DB)
            var validHistory = chatHistory
                .Where(ch => !string.IsNullOrEmpty(ch.question) && 
                            !string.IsNullOrEmpty(ch.answer) &&
                            ch.question != "undefined" && 
                            ch.answer != "undefined")
                .ToList();

            Console.WriteLine($"📊 Load chat history: Total={chatHistory.Count}, Valid={validHistory.Count} for CustomerID={customerId}");

            return Json(new { history = validHistory });
        }
    }
}

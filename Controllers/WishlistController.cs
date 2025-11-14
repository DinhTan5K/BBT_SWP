using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using start.Models;
using start.Data;

public class WishlistToggleRequest {
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }
}

[Authorize(AuthenticationSchemes = "CustomerScheme")]
[Route("[controller]")]
public class WishlistController : Controller {
    private readonly ApplicationDbContext _context;
    public WishlistController(ApplicationDbContext context) { _context = context; }

    // Helper method để lấy CustomerID từ Claims (CustomerScheme)
    private int? GetCustomerId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out int customerId))
            return customerId;
        return null;
    }

    [HttpPost]
    [Route("Toggle")]
    public IActionResult Toggle([FromBody] WishlistToggleRequest req) {
        try {
            int? customerId = GetCustomerId();
            if (customerId == null)
                return Json(new { success = false, error = "Chưa đăng nhập" });
            
            if (req == null || req.ProductId <= 0)
                return Json(new { success = false, error = "Thiếu ProductId hoặc ProductId không hợp lệ" });

            var wish = _context.Wishlist
                .FirstOrDefault(w => w.CustomerID == customerId.Value && w.ProductID == req.ProductId);
            bool isWishlisted;
            if (wish == null) {
                _context.Wishlist.Add(new Wishlist{ CustomerID = customerId.Value, ProductID = req.ProductId });
                isWishlisted = true;
            } else {
                _context.Wishlist.Remove(wish); 
                isWishlisted = false;
            }
            _context.SaveChanges();
            return Json(new { success = true, isWishlisted });
        } catch (Exception ex) {
            return Json(new { success = false, error = $"Lỗi: {ex.Message}" });
        }
    }
}

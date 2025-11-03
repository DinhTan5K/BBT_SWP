using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;

public class WishlistToggleRequest {
    public int ProductId { get; set; }
}

public class WishlistController : Controller {
    private readonly ApplicationDbContext _context;
    public WishlistController(ApplicationDbContext context) { _context = context; }

    [HttpPost]
    public IActionResult Toggle([FromBody] WishlistToggleRequest req) {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, error = "Chưa đăng nhập" });
        if (req == null || req.ProductId == 0)
            return Json(new { success = false, error = "Thiếu ProductId" });

        var wish = _context.Wishlist
            .FirstOrDefault(w => w.CustomerID == customerId.Value && w.ProductID == req.ProductId);
        bool isWishlisted;
        if (wish == null) {
            _context.Wishlist.Add(new Wishlist{ CustomerID = customerId.Value, ProductID = req.ProductId });
            isWishlisted = true;
        } else {
            _context.Wishlist.Remove(wish); isWishlisted = false;
        }
        _context.SaveChanges();
        return Json(new { success = true, isWishlisted });
    }
}

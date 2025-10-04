using Microsoft.AspNetCore.Mvc;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }
#region Items
    

    [HttpGet]
    public IActionResult Items()
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Chưa đăng nhập" });

        var items = _cartService.GetCartItems(customerId.Value);
        return Json(items);
    }
    #endregion

#region Add

    [HttpPost]
    public IActionResult Add([FromBody] AddToCartRequest request)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        if (_cartService.AddToCart(customerId.Value, request, out string message))
            return Json(new { success = true });

        return Json(new { success = false, message });
    }
    #endregion


#region Update
    

    [HttpPost]
    public IActionResult Update([FromBody] UpdateCartRequest request)
    {
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
        if (customerId == null)
            return Json(new { success = false, message = "Bạn chưa đăng nhập" });

        if (_cartService.UpdateCart(customerId.Value, request, out string message))
            return Json(new { success = true });

        return Json(new { success = false, message });
    }
}
#endregion
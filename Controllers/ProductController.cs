using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using start.Models;
using start.Data;
using start.DTOs.Product;
using Microsoft.AspNetCore.Authentication;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ApplicationDbContext _db;

    public ProductController(IProductService productService, ApplicationDbContext db)
    {
        _productService = productService;
        _db = db;
    }

    // Helper method để lấy CustomerID từ Claims (CustomerScheme) - không bắt buộc đăng nhập
    private int? GetCustomerId()
    {
        // Try to authenticate with CustomerScheme
        var authResult = HttpContext.AuthenticateAsync("CustomerScheme").Result;
        if (authResult?.Succeeded == true)
        {
            var userIdClaim = authResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int customerId))
                return customerId;
        }
        return null;
    }

    public IActionResult Product()
    {
        // Initial load - only show empty state, products will load via AJAX
        int? customerId = GetCustomerId();
        List<int> wishIds = new List<int>();
        if (customerId.HasValue)
        {
            wishIds = _db.Wishlist.Where(w => w.CustomerID == customerId.Value).Select(w => w.ProductID).ToList();
        }
        ViewBag.WishlistedIds = wishIds;
        return View(new List<Product>()); // Empty list initially
    }

    [HttpGet]
    public IActionResult FilterProducts([FromQuery] ProductFilterRequest request)
    {
        int? customerId = GetCustomerId();
        List<int> wishIds = new List<int>();
        if (customerId.HasValue)
        {
            wishIds = _db.Wishlist.Where(w => w.CustomerID == customerId.Value).Select(w => w.ProductID).ToList();
        }
        
        var response = _productService.GetFilteredProducts(request, wishIds);
        return Json(response);
    }

    // public IActionResult Detail(int id)
    // {
    //     var product = _productService.GetProductById(id);
    //     if (product == null)
    //         return NotFound();

    //     return View(product);
    // }

    [HttpGet]
    public IActionResult QuickView(int id)
    {
        var product = _productService.GetProductById(id);
        if (product == null) return Content("<p>Sản phẩm không tồn tại</p>", "text/html");
        return PartialView("_ProductQuickViewPartial", product);
    }

    [HttpGet]
    public IActionResult GetCategoryCounts()
    {
        var counts = _productService.GetCategoryProductCounts();
        return Json(counts);
    }
}

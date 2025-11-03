using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;
using start.DTOs.Product;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ApplicationDbContext _db;

    public ProductController(IProductService productService, ApplicationDbContext db)
    {
        _productService = productService;
        _db = db;
    }

    public IActionResult Product()
    {
        // Initial load - only show empty state, products will load via AJAX
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
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
        int? customerId = HttpContext.Session.GetInt32("CustomerID");
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

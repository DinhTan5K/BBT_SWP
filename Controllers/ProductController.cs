using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using start.Data;
using start.DTOs.Product;
using start.Models.ViewModels;
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

    // GET: Product/Detail/{id}
    public IActionResult Detail(int id)
    {
        var product = _productService.GetProductById(id);
        if (product == null)
            return NotFound();

        // Lấy danh sách đánh giá của sản phẩm - Tạm thời dùng mock data
        List<ProductReview> reviews = new List<ProductReview>();
        try 
        {
            reviews = _db.ProductReviews
                .Where(r => r.ProductID == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            // Nếu bảng chưa có, dùng mock data
            reviews = new List<ProductReview>
            {
                new ProductReview
                {
                    ReviewID = 1,
                    ProductID = id,
                    Rating = 5,
                    Comment = "Sản phẩm rất ngon, sẽ mua lại!",
                    CustomerName = "Minh Anh",
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new ProductReview
                {
                    ReviewID = 2,
                    ProductID = id,
                    Rating = 4,
                    Comment = "Đậm vị trà, rất hài lòng.",
                    CustomerName = "Tuấn",
                    CreatedAt = DateTime.Now.AddDays(-3)
                }
            };
        }

        var viewModel = new ProductDetailViewModel
        {
            Product = product,
            Reviews = reviews
        };

        return View(viewModel);
    }

    // POST: Product/AddReview
    [HttpPost]
    [Authorize] // Yêu cầu đăng nhập
    public IActionResult AddReview(int ProductID, int Rating, string Comment)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(Comment))
        {
            ModelState.AddModelError("Comment", "Vui lòng nhập nội dung đánh giá");
            return RedirectToAction("Detail", new { id = ProductID });
        }

        var product = _productService.GetProductById(ProductID);
        if (product == null)
        {
            return NotFound();
        }

        // Lấy thông tin customer từ claims
        int? customerId = GetCustomerId();
        string customerName = User.Identity.Name ?? "Khách hàng";

        var review = new ProductReview
        {
            ProductID = ProductID,
            Rating = Rating,
            Comment = Comment,
            CustomerName = customerName,
            CustomerID = customerId,
            CreatedAt = DateTime.Now
        };

        try
        {
            _db.ProductReviews.Add(review);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            // Nếu database chưa có bảng, chỉ redirect về detail với message
            // Có thể log error ở đây nếu cần
        }

        return RedirectToAction("Detail", new { id = ProductID });
    }

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

using Microsoft.AspNetCore.Mvc;
using start.Models;

public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public IActionResult Product()
    {
        var products = _productService.GetAllProducts();
        return View(products);
    }

    // public IActionResult Detail(int id)
    // {
    //     var product = _productService.GetProductById(id);
    //     if (product == null)
    //         return NotFound();

    //     return View(product);
    // }
}

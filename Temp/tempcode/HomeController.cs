using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Services;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;
    private readonly IStoreService _storeService;
    private readonly INewsService _newsService;
    private readonly IProductService _productService;

    public HomeController(IEmailService emailService, ILogger<HomeController> logger, IStoreService storeService, INewsService newsService, IProductService productService)
    {
        _emailService = emailService;
        _logger = logger;
        _storeService = storeService;
        _newsService = newsService;
        _productService = productService;
    }

  
    public IActionResult Profile()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
    public IActionResult Introduce()
    {
        return View();
    }

     public IActionResult Index()
    {
        var featuredProducts = _productService.GetFeaturedProducts();
        ViewBag.FeaturedProducts = featuredProducts;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }


    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactFormModel());
    }


    public async Task<IActionResult> News()
    {
        var newsList = await _newsService.GetAllAsync();
        return View(newsList);
    }


    public async Task<IActionResult> NewsDetail(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var news = await _newsService.GetByIdAsync(id);
        if (news == null)
        {
            return NotFound();
        }

        return View(news);
    }

    [HttpGet]
    public async Task<IActionResult> Store()
    {
        var stores = await _storeService.GetAllBranchesAsync();
        return View(stores);
    }

    [HttpGet]
    public async Task<IActionResult> StoreFilter(string search, string region)
    {
        var stores = await _storeService.FilterBranchesAsync(search, region);
        return PartialView("_StoreListPartial", stores);
    }

    [HttpGet]
    public async Task<IActionResult> StoreSuggest(string term)
    {
        var suggestions = await _storeService.SuggestBranchNamesAsync(term);
        return Json(suggestions);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

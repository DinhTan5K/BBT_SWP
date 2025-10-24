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

    public IActionResult Login()
    {
        return View();
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            string adminSubject = $"[Liên hệ mới] {model.Name}";
            string adminBody = $@"
                    <p><b>Người gửi:</b> {model.Name}</p>
                    <p><b>Email:</b> {model.Email}</p>
                    <p><b>Điện thoại:</b> {model.Phone}</p>
                    <p><b>Tỉnh/Thành:</b> {model.City}</p>
                    <p><b>Cửa hàng phản hồi:</b> {model.Store}</p>
                    <p><b>Vấn đề:</b> {model.IssueType}</p>
                    <p><b>Nội dung:</b><br/>{model.Message}</p>";

            await _emailService.SendEmailAsync("tant91468@gmail.com", adminSubject, adminBody);
            string userSubject = "Cảm ơn bạn đã liên hệ với Buble Tea";
            string userBody = $@"
                    <p>Xin chào {model.Name},</p>
                    <p>Cảm ơn bạn đã gửi góp ý đến <b>Buble Tea</b>. Dưới đây là nội dung bạn đã gửi:</p>
                    <blockquote>{model.Message}</blockquote>
                    <p>Chúng tôi sẽ phản hồi cho bạn trong thời gian sớm nhất.</p>
                    <p>Trân trọng,<br/>Đội ngũ Buble Tea</p>";

            await _emailService.SendEmailAsync(model.Email, userSubject, userBody);

            TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm.";
            return RedirectToAction("Contact");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý form contact");
            ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi gửi email. Vui lòng thử lại sau.");
            return View(model);
        }
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

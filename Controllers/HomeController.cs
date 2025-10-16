using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;


public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailService _emailService;

      public HomeController(IEmailService emailService, ILogger<HomeController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }


    public IActionResult Login()
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
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "milktea");

        var files = Directory.GetFiles(folderPath);

        var list = files.Select(file =>
            "/img/milktea/" + Path.GetFileName(file)
        ).ToList();

        ViewBag.TraSuaList = list;

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


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

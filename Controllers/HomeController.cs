using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Services;
using System.Net.Mail;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _emailService;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, EmailService emailService)
    {
        _logger = logger;
        _env = env;
        _emailService = emailService;
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Index()
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "milktea");

        var files = Directory.Exists(folderPath) ? Directory.GetFiles(folderPath) : Array.Empty<string>();

        var list = files.Select(file =>
            "/img/milktea/" + Path.GetFileName(file)
        ).ToList();

        ViewBag.TraSuaList = list;

        return View();
    }

    public IActionResult Product()
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "milktea");

        var files = Directory.Exists(folderPath) ? Directory.GetFiles(folderPath) : Array.Empty<string>();

        var list = files.Select(file =>
            "/img/milktea/" + Path.GetFileName(file)
        ).ToList();

        ViewBag.TraSuaList = list;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // GET: Contact (hiển thị form)
    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactFormModel());
    }

    // POST: Contact (xử lý gửi form)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Tạo nội dung gửi cho admin
        string adminSubject = $"[Liên hệ mới] {model.Name}";
        string adminBody = $@"
            <p><b>Người gửi:</b> {model.Name}</p>
            <p><b>Email:</b> {model.Email}</p>
            <p><b>Điện thoại:</b> {model.Phone}</p>
            <p><b>Tỉnh/Thành:</b> {model.City}</p>
            <p><b>Cửa hàng phản hồi:</b> {model.Store}</p>
            <p><b>Vấn đề:</b> {model.IssueType}</p>
            <p><b>Nội dung:</b><br/>{model.Message}</p>
        ";

        try
        {
            // Gửi email tới admin (replyTo = email user để admin trả lời dễ)
            await _emailService.SendToAdminAsync(adminSubject, adminBody, replyTo: model.Email);

            // Kiểm tra email user hợp lệ (format) trước khi gửi auto-reply
            bool emailValid = IsValidEmail(model.Email);

            if (emailValid)
            {
                string userSubject = "Cảm ơn bạn đã liên hệ Buble Tea";
                string userBody = $@"
                    <p>Xin chào {model.Name},</p>
                    <p>Cảm ơn bạn đã gửi góp ý đến <b>Buble Tea</b>. Dưới đây là nội dung bạn đã gửi:</p>
                    <blockquote>{model.Message}</blockquote>
                    <p>Chúng tôi sẽ phản hồi cho bạn trong thời gian sớm nhất.</p>
                    <p>Trân trọng,<br/>Đội ngũ Buble Tea</p>
                ";
                await _emailService.SendEmailAsync(model.Email, userSubject, userBody);
                TempData["ContactSuccess"] = "Cảm ơn bạn đã góp ý cho Buble Tea, vui lòng kiểm tra email để nhận phản hồi từ chúng tôi.";
            }
            else
            {
                // Nếu email user sai định dạng: không gửi auto-reply, vẫn show thank-you, kèm cảnh báo
                TempData["ContactSuccess"] = "Cảm ơn bạn đã góp ý cho Buble Tea";
                TempData["ContactWarning"] = "Địa chỉ email bạn cung cấp không hợp lệ nên chúng tôi không thể gửi email xác nhận tới bạn. Chúng tôi vẫn đã nhận được phản hồi và sẽ liên hệ theo thông tin khác nếu cần.";
            }

            // PRG pattern
            return RedirectToAction("Contact");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý form contact");
            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi gửi phản hồi. Vui lòng thử lại sau.");
            return View(model);
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Kiểm tra format email
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

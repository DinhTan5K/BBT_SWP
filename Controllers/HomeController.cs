using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Services;
using System.Net.Mail;
using System.Threading.Tasks;
using start.Data;                  // <-- ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, EmailService emailService, ApplicationDbContext db)
    {
        _logger = logger;
        _env = env;
        _emailService = emailService;
        _db = db;
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
        // load regions for dropdown
        var regions = _db.Regions
                         .AsNoTracking()
                         .OrderBy(r => r.RegionID)
                         .Select(r => new { r.RegionID, r.RegionName })
                         .ToList();

        ViewBag.Regions = regions; // small anonymous list; view sẽ render

        return View(new ContactFormModel());
    }

    // Endpoint: get distinct cities for a region (AJAX)
    [HttpGet]
    public IActionResult GetCities(int regionId)
    {
        var cities = _db.Branches
                        .AsNoTracking()
                        .Where(b => b.RegionID == regionId && !string.IsNullOrEmpty(b.City))
                        .Select(b => b.City!)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

        return Json(cities);
    }

    // Endpoint: get branches for a city (AJAX)
    [HttpGet]
    public IActionResult GetBranches(string city)
    {
        if (string.IsNullOrEmpty(city))
            return Json(new object[0]);

        var branches = _db.Branches
                          .AsNoTracking()
                          .Where(b => b.City == city)
                          .Select(b => new { b.Id, b.Name })
                          .OrderBy(b => b.Name)
                          .ToList();

        return Json(branches);
    }

    // POST: Contact (xử lý gửi form)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            // reload regions if invalid so view has dropdown data
            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionID)
                             .Select(r => new { r.RegionID, r.RegionName })
                             .ToList();
            ViewBag.Regions = regions;
            return View(model);
        }

        // Resolve names from ids for email body
        string regionName = "";
        string branchName = "";

        if (model.RegionId.HasValue)
        {
            var r = await _db.Regions.FindAsync(model.RegionId.Value);
            regionName = r?.RegionName ?? "";
        }

        if (model.StoreId.HasValue)
        {
            var br = await _db.Branches.FindAsync(model.StoreId.Value);
            branchName = br?.Name ?? "";
        }

        // Tạo nội dung gửi cho admin
        string adminSubject = $"[Liên hệ mới] {model.Name}";
        string adminBody = $@"
            <p><b>Người gửi:</b> {model.Name}</p>
            <p><b>Email:</b> {model.Email}</p>
            <p><b>Điện thoại:</b> {model.Phone}</p>
            <p><b>Vùng/Miền:</b> {regionName}</p>
            <p><b>Tỉnh/Thành:</b> {model.City}</p>
            <p><b>Cửa hàng phản hồi:</b> {branchName}</p>
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

            // reload regions before return view
            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionID)
                             .Select(r => new { r.RegionID, r.RegionName })
                             .ToList();
            ViewBag.Regions = regions;

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

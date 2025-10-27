using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Services;

[Route("Contact")]
public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _db;

    public ContactController(ILogger<ContactController> logger, EmailService emailService, ApplicationDbContext db)
    {
        _logger = logger;
        _emailService = emailService;
        _db = db;
    }

    [HttpGet]
    public IActionResult Contact()
    {
        var regions = _db.Regions
                         .AsNoTracking()
                         .OrderBy(r => r.RegionID)
                         .Select(r => new { r.RegionID, r.RegionName })
                         .ToList();

        ViewBag.Regions = regions;
        return View(new ContactFormModel());
    }

    [HttpGet("GetCities")]
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

    [HttpGet("GetBranches")]
    public IActionResult GetBranches(string city)
    {
        if (string.IsNullOrEmpty(city))
            return Json(new object[0]);

        var branches = _db.Branches
                          .AsNoTracking()
                          .Where(b => b.City == city)
                          .Select(b => new { id = b.BranchID, name = b.Name })
                          .OrderBy(b => b.name)
                          .ToList();

        return Json(branches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        if (!ModelState.IsValid)
        {
            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionID)
                             .Select(r => new { r.RegionID, r.RegionName })
                             .ToList();
            ViewBag.Regions = regions;
            return View(model);
        }

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

        try
        {
            string adminSubject = $"[Liên hệ mới] {model.Name}";
            string adminBody = $@"
                    <p><b>Người gửi:</b> {model.Name}</p>
                    <p><b>Email:</b> {model.Email}</p>
                    <p><b>Điện thoại:</b> {model.Phone}</p>
                    <p><b>Vùng/Miền:</b> {regionName}</p>
                    <p><b>Tỉnh/Thành:</b> {model.City}</p>
                     <p><b>Cửa hàng phản hồi:</b> {branchName}</p>
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
            var regions = _db.Regions
                             .AsNoTracking()
                             .OrderBy(r => r.RegionID)
                             .Select(r => new { r.RegionID, r.RegionName })
                             .ToList();
            ViewBag.Regions = regions;
            return View(model);
        }
    }
}



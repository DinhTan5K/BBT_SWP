using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;
using start.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using start.Services;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;
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

        var files = Directory.GetFiles(folderPath);

        var list = files.Select(file =>
            "/img/milktea/" + Path.GetFileName(file)
        ).ToList();

        ViewBag.TraSuaList = list;

        return View();
    }




    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

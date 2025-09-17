using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using start.Models;

namespace start.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _env;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (username == "ad" && password == "1")
        {
            return RedirectToAction("Privacy", "Home");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
        return View("Login");
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



    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

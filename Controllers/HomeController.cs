using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;


public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;


    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _logger = logger;
        _context = context;

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

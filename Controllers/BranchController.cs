using Microsoft.AspNetCore.Mvc;
using start.Data;

namespace start.Controllers
{
    public class BranchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BranchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var branches = _context.Branches.ToList();
            return View(branches);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var branches = _context.Branches.ToList();
            return Json(branches);
        }
    }
}

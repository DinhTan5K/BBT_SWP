using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var branches = await _context.Branches
                .Select(b => new
                {
                    branchID = b.Id,    // ⚡ dùng Id, không phải BranchID
                    name = b.Name,
                    address = b.Address,
                    latitude = b.Latitude,
                    longitude = b.Longitude
                })
                .ToListAsync();

            return Json(branches);
        }
    }
}

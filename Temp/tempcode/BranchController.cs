using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using start.Data;
using System.Threading.Tasks;


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
            try
            {
                var branches = await _context.Branches
                    .Select(b => new
                    {
                        branchID = b.BranchID,
                        name = b.Name,
                        address = b.Address,
                        latitude = b.Latitude,
                        longitude = b.Longitude
                    })
                    .ToListAsync();

                return Ok(branches);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Branch/GetAll failed: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
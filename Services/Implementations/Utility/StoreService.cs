using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using System.Text;

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;

    public StoreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Branch>> GetAllBranchesAsync()
    {
        return await _context.Branches.ToListAsync();
    }

    public async Task<List<Branch>> FilterBranchesAsync(string search, string region)
    {
        var query = _context.Branches.AsQueryable();

        if (!string.IsNullOrEmpty(region) && int.TryParse(region, out int regionID))
        {
            query = query.Where(s => s.RegionID == regionID);
        }

        if (!string.IsNullOrEmpty(search))
        {
            const string collation = "SQL_Latin1_General_CP1_CI_AI";
            query = query.Where(s =>
                (s.Name != null && EF.Functions.Collate(s.Name, collation).Contains(search)) ||
                (s.Address != null && EF.Functions.Collate(s.Address, collation).Contains(search))
            );
        }

        return await query.ToListAsync();
    }

    public async Task<List<string>> SuggestBranchNamesAsync(string term)
    {
        if (string.IsNullOrEmpty(term)) return new List<string>();

        const string collation = "SQL_Latin1_General_CP1_CI_AI";
        return await _context.Branches
            .Where(s => s.Name != null && EF.Functions.Collate(s.Name, collation).Contains(term))
            .Select(s => s.Name!)
            .Distinct()
            .Take(10)
            .ToListAsync();
    }
}
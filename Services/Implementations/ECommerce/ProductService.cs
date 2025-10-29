using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Product> GetAllProducts()
    {
        return _context.Products
            .Include(p => p.ProductSizes)
            .OrderBy(p => p.CategoryID)
            .ToList();
    }

    public Product? GetProductById(int id)
    {
        return _context.Products
            .Include(p => p.ProductSizes)
            .FirstOrDefault(p => p.ProductID == id);
    }

     public List<Product> GetFeaturedProducts(int take = 8)
        {
            return _context.Products
                .Include(p => p.ProductSizes)
                .Where(p => p.IsActive)
                .Take(take)
                .ToList();
        }
}

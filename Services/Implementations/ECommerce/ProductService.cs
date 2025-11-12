using Microsoft.EntityFrameworkCore;
using start.Data;
using start.Models;
using start.DTOs.Product;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Product? GetProductById(int id)
    {
        return _context.Products
            .Include(p => p.ProductSizes)
            .Include(p => p.Category)
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

    public ProductFilterResponse GetFilteredProducts(ProductFilterRequest request, List<int> wishlistedIds)
    {
        var query = _context.Products
            .Include(p => p.ProductSizes)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Filter by wishlist
        if (request.WishlistOnly && wishlistedIds.Any())
        {
            query = query.Where(p => wishlistedIds.Contains(p.ProductID));
        }
        else if (request.WishlistOnly && !wishlistedIds.Any())
        {
            // If wishlist filter is on but no wishlisted items, return empty
            query = query.Where(p => false);
        }

        // Filter by category
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryID == request.CategoryId.Value);
        }

        // Search by name
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower().Trim();
            query = query.Where(p => p.ProductName != null && p.ProductName.ToLower().Contains(term));
        }

        // Filter by price range
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.ProductSizes.Any(ps => 
                (!request.MinPrice.HasValue || ps.Price >= request.MinPrice.Value) &&
                (!request.MaxPrice.HasValue || ps.Price <= request.MaxPrice.Value)
            ));
        }

        // Apply sorting
        query = request.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.ProductSizes.Min(ps => ps.Price)),
            "price_desc" => query.OrderByDescending(p => p.ProductSizes.Min(ps => ps.Price)),
            "name_asc" => query.OrderBy(p => p.ProductName),
            "name_desc" => query.OrderByDescending(p => p.ProductName),
            "newest" => query.OrderByDescending(p => p.ProductID),
            _ => query.OrderBy(p => p.CategoryID).ThenBy(p => p.ProductName)
        };

        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        // Pagination
        var products = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var productDtos = products.Select(p => new ProductItemDto
        {
            ProductID = p.ProductID,
            ProductName = p.ProductName ?? "",
            Image_Url = p.Image_Url,
            Description = p.Description,
            CategoryID = p.CategoryID,
            CategoryName = p.Category?.CategoryName,
            MinPrice = p.ProductSizes.Any() ? p.ProductSizes.Min(ps => ps.Price) : 0,
            ProductSizes = p.ProductSizes.Select(ps => new ProductSizeDto
            {
                ProductSizeID = ps.ProductSizeID,
                Size = ps.Size,
                Price = ps.Price
            }).ToList(),
            IsWishlisted = wishlistedIds.Contains(p.ProductID)
        }).ToList();

        return new ProductFilterResponse
        {
            Products = productDtos,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = request.Page
        };
    }

    public Dictionary<int, int> GetCategoryProductCounts()
    {
        return _context.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryID)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

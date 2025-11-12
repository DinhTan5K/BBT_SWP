using start.Models;
using start.DTOs.Product;

public interface IProductService
{
    Product? GetProductById(int id);
    List<Product> GetFeaturedProducts(int take = 8);
    ProductFilterResponse GetFilteredProducts(ProductFilterRequest request, List<int> wishlistedIds);
    Dictionary<int, int> GetCategoryProductCounts();
}

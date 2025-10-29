using start.Models;

public interface IProductService
{
    List<Product> GetAllProducts();
    Product? GetProductById(int id);
    List<Product> GetFeaturedProducts(int take = 8);
}

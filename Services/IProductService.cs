using start.Models;

public interface IProductService
{
    List<Product> GetAllProducts();
    Product? GetProductById(int id);
}

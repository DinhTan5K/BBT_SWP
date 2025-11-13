
namespace start.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public int ProductID => Product.ProductID;
        public double AverageRating => Reviews.Count > 0 ? Reviews.Average(r => r.Rating) : 0;
    }
}

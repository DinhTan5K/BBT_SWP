namespace start.DTOs.Product
{
    public class ProductFilterRequest
    {
        public int? CategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; } // "price_asc", "price_desc", "name_asc", "name_desc", "newest"
        public bool WishlistOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }

    public class ProductFilterResponse
    {
        public List<ProductItemDto> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    public class ProductItemDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Image_Url { get; set; }
        public string? Description { get; set; }
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public decimal MinPrice { get; set; }
        public List<ProductSizeDto> ProductSizes { get; set; } = new();
        public bool IsWishlisted { get; set; }
    }

    public class ProductSizeDto
    {
        public int ProductSizeID { get; set; }
        public string Size { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}





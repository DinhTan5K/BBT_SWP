using start.DTOs;
using start.DTOs.Product;
public class SentRequestDetail {
    public int Id { get; set; }
    public RequestCategory Category { get; set; }
    public int RequestType { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public int Status { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Branch fields
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? BranchPhone { get; set; }

    // EmployeeBranch fields
    public string? EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public int? RegionId { get; set; }

    

    // Category
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

   
    // Product
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
   
    public string? ProductDescription { get; set; }
    public string? ProductImageUrl { get; set; }
    public int? ProductCategoryId { get; set; }
    public string? ProductCategoryName { get; set; }
    public List<ProductSizeDto>? ProductSizes { get; set; }

    public bool CanDelete { get; set; } = false;

    
}
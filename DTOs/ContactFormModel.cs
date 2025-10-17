using System.ComponentModel.DataAnnotations;
public class ContactFormModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    public int? RegionId { get; set; }

    public string City { get; set; } = string.Empty; // Tỉnh/Thành
    public int? StoreId { get; set; }

    public string Store { get; set; } = string.Empty; // Cửa hàng phản hồi

    public string IssueType { get; set; } = string.Empty; // Loại vấn đề

    [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
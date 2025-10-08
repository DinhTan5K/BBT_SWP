public class PromoValidationResult
{
    // Dùng cho trường hợp thành công
    public decimal FinalTotal { get; set; }
    public decimal FinalShippingFee { get; set; }
    public decimal TotalDiscount { get; set; }
    public List<string> AppliedMessages { get; set; } = new List<string>();
    public List<string> SuccessfullyAppliedCodes { get; set; } = new List<string>();

    // Dùng cho trường hợp có lỗi
    public string? ErrorMessage { get; set; }
    public string? InvalidCode { get; set; }
    public string? CurrentShippingCode { get; set; } // Dành riêng cho lỗi trùng mã ship

    // Một thuộc tính để kiểm tra nhanh thành công hay thất bại
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}
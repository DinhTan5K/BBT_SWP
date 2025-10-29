public class PromoCodeResponse
{
    public decimal FinalTotal { get; set; }
    public decimal FinalShippingFee { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public List<string>? AppliedMessages { get; set; }
    public List<string>?SuccessfullyAppliedCodes { get; set; }
    public string? ErrorMessage { get; set; }
}
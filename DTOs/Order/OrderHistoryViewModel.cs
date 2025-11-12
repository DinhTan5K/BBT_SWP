public class OrderHistoryViewModel
{
    public List<OrderSummaryViewModel>? Orders { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}    
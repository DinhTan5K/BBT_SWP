using start.DTOs;
public class SentRequestListItem {
    public int Id { get; set; }
    public RequestCategory Category { get; set; }
    public int RequestType { get; set; }
    public string? RequestTypeLabel { get; set; }
    public string? ContentSummary { get; set; }      // plain text (tìm kiếm)
    public string? ContentHtml { get; set; }         // already HTML-encoded + <br/> để hiển thị
    public DateTime RequestedAt { get; set; }
    public int Status { get; set; }
    public string? StatusLabel { get; set; }
    public string? RequestedBy { get; set; }
}
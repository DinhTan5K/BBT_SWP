namespace start.DTOs
{
    public class MomoRefundResponse
    {
        public string partnerCode { get; set; }
        public string orderId { get; set; }
        public string requestId { get; set; }
        public long amount { get; set; }
        public long transId { get; set; }
        public int resultCode { get; set; }
        public string message { get; set; }
        public long responseTime { get; set; }
    }
}
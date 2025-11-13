 public class PromoValidationRequest
    {
        public List<string>? Codes { get; set; }
        public decimal ItemsTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public int? UserId { get; set; } // Optional: để check user đã dùng discount chưa
    }

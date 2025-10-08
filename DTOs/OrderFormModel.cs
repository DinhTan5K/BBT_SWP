
    public class OrderFormModel
    {
        public int BranchID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? DetailAddress { get; set; }
        public string? Note { get; set; }
        public decimal ShippingFee { get; set; } = 0;
        public string? PromoCode { get; set; }
        public string? Payment { get; set; }
    }


 public class RevenueTrendDto
    {
        public DateTime Date { get; set; }

    public string DateLabel => Date.ToString("dd/MM");
         
        public decimal Revenue { get; set; }
    }
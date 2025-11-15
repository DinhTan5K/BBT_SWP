namespace start.DTOs.Employee
{
    public class CheckInModalViewModel
    {
        public bool CanStart { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsCheckIn { get; set; }
        public int? WorkScheduleId { get; set; }
    }
}


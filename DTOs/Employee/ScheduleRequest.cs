public class ScheduleRequest
{
    public DateTime WorkDate { get; set; }
    public string? ShiftType { get; set; }
}

public class ScheduleIdRequest
{
    public int Id { get; set; }
}
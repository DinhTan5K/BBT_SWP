public class ShiftService
{
    public (string Shift, DateTime Start, DateTime End) GetShift(DateTime today, string? shiftName)
    {
        string shift = shiftName ?? "Morning";

        DateTime start, end;

        if (shift.Equals("Morning", StringComparison.OrdinalIgnoreCase))
        {
            start = today.AddHours(0);
            end = today.AddHours(14).AddMinutes(59).AddSeconds(59);
        }
        else
        {
            start = today.AddHours(15);
            end = today.AddHours(23).AddMinutes(59).AddSeconds(59);
        }

        return (shift, start, end);
    }
    public string? GetCurrentShift()
    {
        var now = DateTime.Now.TimeOfDay;

        if (now < new TimeSpan(15, 0, 0))
            return "Sáng";

        if (now < new TimeSpan(24, 0, 0))
            return "Tối";

        return null;
    }

}

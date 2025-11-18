using System;

public static class ShiftTimeHelper
{
    // Định nghĩa giờ bắt đầu và kết thúc cho mỗi ca
    private static readonly TimeSpan MorningShiftStart = new TimeSpan(7, 30, 0); // 7:30 AM
    private static readonly TimeSpan NightShiftStart = new TimeSpan(15, 0, 0); // 3:00 PM

    // Khoảng thời gian cho phép check-in (ví dụ: 30 phút trước và sau giờ bắt đầu)
    private const int CheckInWindowMinutes = 30;

    public static bool CanCheckIn(DateTime currentTime, DateTime workDate, string? shiftName, out string message)
    {
        message = "";
        TimeSpan shiftStartTime;

        // Xác định giờ bắt đầu ca dựa trên tên ca
        if ("Sáng".Equals(shiftName, StringComparison.OrdinalIgnoreCase))
        {
            shiftStartTime = MorningShiftStart;
        }
        else if ("Tối".Equals(shiftName, StringComparison.OrdinalIgnoreCase))
        {
            shiftStartTime = NightShiftStart;
        }
        else
        {
            message = "Ca làm việc không hợp lệ.";
            return false;
        }

        // Tính toán khoảng thời gian check-in hợp lệ
        var checkInStart = workDate.Date.Add(shiftStartTime).AddMinutes(-CheckInWindowMinutes);
        var checkInEnd = workDate.Date.Add(shiftStartTime).AddMinutes(CheckInWindowMinutes);

        if (currentTime < checkInStart)
        {
            message = $"Bạn chỉ có thể check-in từ {checkInStart:HH:mm}.";
            return false;
        }
        if (currentTime > checkInEnd)
        {
            message = $"Đã quá giờ check-in cho ca này (trễ nhất là {checkInEnd:HH:mm}).";
            return false;
        }

        return true;
    }
}

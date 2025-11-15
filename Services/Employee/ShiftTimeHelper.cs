using System;

public static class ShiftTimeHelper
{
    // Định nghĩa giờ bắt đầu và kết thúc cho mỗi ca
    private static readonly TimeSpan MorningShiftStart = new TimeSpan(8, 0, 0); // 8:00 AM (changed from 7:30 AM)
    private static readonly TimeSpan MorningShiftEnd = new TimeSpan(15, 0, 0);   // 3:00 PM (15:00)
    private static readonly TimeSpan NightShiftStart = new TimeSpan(15, 0, 0);   // 3:00 PM (15:00)
    private static readonly TimeSpan NightShiftEnd = new TimeSpan(22, 0, 0);     // 10:00 PM (22:00)

    // Thời gian cho phép trễ (10 phút)
    private const int LateAllowanceMinutes = 10;

    /// <summary>
    /// Kiểm tra xem có thể check-in không
    /// Check-in có thể bắt đầu từ giờ bắt đầu ca và trễ tối đa 10 phút
    /// </summary>
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
        // Check-in bắt đầu từ giờ bắt đầu ca và có thể trễ tối đa 10 phút
        var checkInStart = workDate.Date.Add(shiftStartTime);
        var checkInEnd = workDate.Date.Add(shiftStartTime).AddMinutes(LateAllowanceMinutes);

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

    /// <summary>
    /// Kiểm tra xem có thể check-out không
    /// Check-out phải đúng giờ kết thúc ca và có thể trễ tối đa 10 phút
    /// </summary>
    public static bool CanCheckOut(DateTime currentTime, DateTime workDate, string? shiftName, out string message)
    {
        message = "";
        TimeSpan shiftEndTime;

        // Xác định giờ kết thúc ca dựa trên tên ca
        if ("Sáng".Equals(shiftName, StringComparison.OrdinalIgnoreCase))
        {
            shiftEndTime = MorningShiftEnd; // 15:00
        }
        else if ("Tối".Equals(shiftName, StringComparison.OrdinalIgnoreCase))
        {
            shiftEndTime = NightShiftEnd; // 22:00
        }
        else
        {
            message = "Ca làm việc không hợp lệ.";
            return false;
        }

        // Tính toán khoảng thời gian check-out hợp lệ
        // Check-out bắt đầu từ giờ kết thúc ca và có thể trễ tối đa 10 phút
        var checkOutStart = workDate.Date.Add(shiftEndTime);
        var checkOutEnd = workDate.Date.Add(shiftEndTime).AddMinutes(LateAllowanceMinutes);

        if (currentTime < checkOutStart)
        {
            message = $"Bạn chỉ có thể check-out từ {checkOutStart:HH:mm}.";
            return false;
        }
        if (currentTime > checkOutEnd)
        {
            message = $"Đã quá giờ check-out cho ca này (trễ nhất là {checkOutEnd:HH:mm}).";
            return false;
        }

        return true;
    }
}
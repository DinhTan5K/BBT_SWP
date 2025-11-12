using System;

namespace start.Services
{
    /// <summary>
    /// Helper class kiểm tra ca làm việc (chỉ xét theo ngày làm việc)
    /// </summary>
    public static class ShiftTimeHelper
    {
        /// <summary>
        /// Trả về ngày bắt đầu ca (bằng ngày làm việc)
        /// </summary>
        public static DateTime GetShiftStartDateTime(DateTime workDate, string? shiftName)
        {
            // Không quan trọng ca là gì nữa, chỉ dùng ngày
            return workDate.Date;
        }

        /// <summary>
        /// Trả về ngày kết thúc ca (bằng cuối ngày làm việc, 23:59:59)
        /// </summary>
        public static DateTime GetShiftEndDateTime(DateTime workDate, string? shiftName)
        {
            return workDate.Date.AddDays(1).AddTicks(-1); // 23:59:59 của cùng ngày
        }

        /// <summary>
        /// Kiểm tra xem có thể check-in được không (chỉ xét ngày)
        /// </summary>
        public static bool CanCheckIn(DateTime currentTime, DateTime workDate, string? shiftName, out string message)
        {
            var startOfDay = workDate.Date;
            var endOfDay = workDate.Date.AddDays(1).AddTicks(-1);

            if (currentTime.Date < startOfDay)
            {
                message = $"Chưa đến ngày làm việc ({workDate:dd/MM/yyyy}).";
                return false;
            }

            if (currentTime.Date > workDate.Date)
            {
                message = $"Đã qua ngày làm việc ({workDate:dd/MM/yyyy}).";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Hiển thị thông tin ca làm (chỉ ngày)
        /// </summary>
        public static string GetShiftTimeDisplay(string? shiftName)
        {
            // Vì không còn chia giờ, chỉ hiển thị "Trong ngày làm việc"
            return "Trong ngày làm việc";
        }
    }
}

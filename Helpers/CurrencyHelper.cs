using System.Globalization;

namespace start.Helpers
{
    public static class CurrencyHelper
    {
        /// <summary>
        /// Format số tiền thành chuỗi VNĐ (ví dụ: 100000 -> "100,000 VNĐ")
        /// </summary>
        public static string FormatVND(decimal? amount)
        {
            if (amount == null) return "0 VNĐ";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture) + " VNĐ";
        }

        /// <summary>
        /// Format số tiền thành chuỗi VNĐ không có đơn vị (ví dụ: 100000 -> "100,000")
        /// </summary>
        public static string FormatVNDNoUnit(decimal? amount)
        {
            if (amount == null) return "0";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format số tiền thành chuỗi với ký hiệu đ (ví dụ: 100000 -> "100,000 đ")
        /// </summary>
        public static string FormatVNDShort(decimal? amount)
        {
            if (amount == null) return "0 đ";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture) + " đ";
        }
    }
}


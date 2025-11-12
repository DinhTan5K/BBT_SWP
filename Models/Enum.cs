using System.ComponentModel.DataAnnotations;

namespace start.Models
{
    /// <summary>
    /// Loại yêu cầu: Add (Thêm mới), Edit (Sửa), Delete (Xóa)
    /// </summary>
    public enum RequestType
    {
        Add = 0,    // Thêm mới
        Edit = 1,   // Sửa
        Delete = 2  // Xóa
    }

    /// <summary>
    /// Trạng thái yêu cầu: Pending (Chờ duyệt), Approved (Đã duyệt), Rejected (Từ chối)
    /// </summary>
    public enum RequestStatus
    {
        Pending = 0,    // Chờ duyệt
        Approved = 1,   // Đã duyệt
        Rejected = 2    // Từ chối
    }

    /// <summary>
    /// Loại giảm giá: Percentage, FixedAmount, FreeShipping, FixedShippingDiscount, PercentShippingDiscount
    /// </summary>
    public enum DiscountType
    {
        [Display(Name = "Percentage")]
        Percentage = 0,
        [Display(Name = "Fixed Amount")]
        FixedAmount = 1,
        [Display(Name = "Free Shipping")]
        FreeShipping = 2,
        [Display(Name = "Fixed Shipping Discount")]
        FixedShippingDiscount = 3,
        [Display(Name = "Percent Shipping Discount")]
        PercentShippingDiscount = 4
    }
}
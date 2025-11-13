using System.ComponentModel.DataAnnotations;

namespace start.DTOs
{
    public class ApplyDiscountRequest
    {
        [Required(ErrorMessage = "Mã giảm giá không được để trống")]
        public string Code { get; set; } = string.Empty;
    }
}

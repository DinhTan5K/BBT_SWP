using System;
using System.ComponentModel.DataAnnotations;

namespace start.Models
{
    public class SalaryAdjustmentDto
    {
        [Required(ErrorMessage = "Thiếu mã nhân viên")]
        public string EmployeeID { get; set; } = string.Empty;
        [Required(ErrorMessage = "Số tiền không được bỏ trống")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Loại điều chỉnh bắt buộc")]
        [RegularExpression("^(Bonus|Penalty)$", ErrorMessage = "Loại điều chỉnh không hợp lệ")]
        public string Type { get; set; } = "Bonus"; 

        [Required(ErrorMessage = "Lý do không được bỏ trống")]
        [StringLength(255, ErrorMessage = "Lý do không vượt quá 255 ký tự")]
        public string Reason { get; set; } = string.Empty;

        public DateTime AdjustmentDate { get; set; } = DateTime.Now;
    }
}

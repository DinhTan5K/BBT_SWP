using System.ComponentModel.DataAnnotations;
public class ResetPasswordViewModel
{
    public string Email { get; set; } = "";
    public string OtpCode { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(7, ErrorMessage = "Mật khẩu mới phải dài hơn 6 ký tự.")]
    public string NewPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmNewPassword { get; set; } = "";
}

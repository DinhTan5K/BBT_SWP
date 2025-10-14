using System.ComponentModel.DataAnnotations;

public class SetPasswordModel
{
    [Required]
    [MinLength(7, ErrorMessage = "Mật khẩu phải dài hơn 6 ký tự.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}



public class ResetPasswordViewModel
{
    public string Email { get; set; } = "";
    public string OtpCode { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmNewPassword { get; set; } = "";
}

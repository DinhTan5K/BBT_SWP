using System.ComponentModel.DataAnnotations;
public class EditEmployeeProfile
{
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? Gender { get; set; }
    public string? Ethnicity { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? EmergencyPhone1 { get; set; }
    public string? EmergencyPhone2 { get; set; }
    public string? CurrentAvatarUrl { get; set; }
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
    public string? ConfirmNewPassword { get; set; }
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool any = !string.IsNullOrWhiteSpace(CurrentPassword)
                || !string.IsNullOrWhiteSpace(NewPassword)
                || !string.IsNullOrWhiteSpace(ConfirmNewPassword);

        if (any)
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword))
                yield return new ValidationResult("Vui lòng nhập mật khẩu hiện tại.", new[] { nameof(CurrentPassword) });

            if (string.IsNullOrWhiteSpace(NewPassword))
                yield return new ValidationResult("Vui lòng nhập mật khẩu mới.", new[] { nameof(NewPassword) });

            if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
                yield return new ValidationResult("Vui lòng xác nhận mật khẩu.", new[] { nameof(ConfirmNewPassword) });

            if (!string.IsNullOrWhiteSpace(NewPassword) &&
                !string.IsNullOrWhiteSpace(ConfirmNewPassword) &&
                NewPassword != ConfirmNewPassword)
                yield return new ValidationResult("Xác nhận mật khẩu không khớp.", new[] { nameof(ConfirmNewPassword) });

            if (!string.IsNullOrWhiteSpace(NewPassword) && NewPassword.Length < 6)
                yield return new ValidationResult("Mật khẩu mới tối thiểu 6 ký tự.", new[] { nameof(NewPassword) });
        }
    }
}
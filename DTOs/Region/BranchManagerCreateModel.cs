using System;
using System.ComponentModel.DataAnnotations;

namespace start.DTOs
{
    public class BranchManagerCreateModel
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(200)]
        public string? City { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày tuyển dụng")]
        public DateTime HireDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(2)]
        public string RoleID { get; set; } = "BM";

        public bool IsActive { get; set; } = true;

        [StringLength(60)]
        public string? Nationality { get; set; }

        [StringLength(60)]
        public string? Ethnicity { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(200, MinimumLength = 6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}

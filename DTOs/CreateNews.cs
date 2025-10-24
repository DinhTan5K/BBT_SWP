using System.ComponentModel.DataAnnotations;

public class CreateNews
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ImageFile { get; set; } 
}
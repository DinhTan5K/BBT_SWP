using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("ShiftReport")]
    public class ShiftReport
    {
        [Key]
        public int ReportID { get; set; }

        [MaxLength(255)]
        public string? Excel_Url { get; set; }   // Đường dẫn file Excel lưu trên server

        public string? Report_Img { get; set; }  // Ảnh biểu đồ (base64 hoặc URL)
        [Required]
        public DateTime Day { get; set; } = DateTime.Today;

        [Required]
        public DateTime LastUpdate { get; set; } = DateTime.Now; // Lần cập nhật gần nhất

        [Required]
        [MaxLength(10)]
        [RegularExpression(@"^(Sáng|Tối)$", ErrorMessage = "Ca làm chỉ có thể là 'Sáng' hoặc 'Tối'.")]
        public string Shift { get; set; } = "Sáng"; // Ca làm (Sáng / Tối)

        [ForeignKey("Branch")]
        public int BranchID { get; set; } // Liên kết tới chi nhánh

        public Branch? Branch { get; set; } // Navigation property
    }
}

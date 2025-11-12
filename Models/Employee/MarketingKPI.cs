using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("MarketingKPI")]
    public class MarketingKPI
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string EmployeeID { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime KpiMonth { get; set; } // Tháng tính KPI (luôn là ngày 01)

        // Số lượng News Requests
        public int TotalNewsRequests { get; set; }
        public int ApprovedNewsRequests { get; set; }
        public int RejectedNewsRequests { get; set; }
        public int PendingNewsRequests { get; set; }

        // Số lượng Discount Requests
        public int TotalDiscountRequests { get; set; }
        public int ApprovedDiscountRequests { get; set; }
        public int RejectedDiscountRequests { get; set; }
        public int PendingDiscountRequests { get; set; }

        // Tỷ lệ approve
        [Column(TypeName = "decimal(5,2)")]
        public decimal NewsApproveRate { get; set; } // Tỷ lệ % news được approve

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountApproveRate { get; set; } // Tỷ lệ % discount được approve

        [Column(TypeName = "decimal(5,2)")]
        public decimal OverallApproveRate { get; set; } // Tỷ lệ approve tổng thể

        // Điểm KPI (0-100)
        [Column(TypeName = "decimal(5,2)")]
        public decimal KPIScore { get; set; }

        // Trạng thái đạt KPI
        public bool IsKPIAchieved { get; set; } // true nếu KPIScore >= TargetScore

        [Column(TypeName = "decimal(5,2)")]
        public decimal TargetScore { get; set; } = 70.0m; // Điểm KPI mục tiêu (mặc định 70%)

        // Bonus dựa trên KPI
        [Column(TypeName = "decimal(18,2)")]
        public decimal KPIBonus { get; set; } // Thưởng KPI

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }
    }
}


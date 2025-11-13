using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("ChatHistory")]
    public class ChatHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChatHistoryID { get; set; }

        public int? CustomerID { get; set; } // Nullable để hỗ trợ cả user chưa đăng nhập

        [Required]
        [StringLength(1000)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "ntext")]
        public string Answer { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }
    }
}
























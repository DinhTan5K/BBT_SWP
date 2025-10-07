using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("Branch")]
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        public int RegionID { get; set; }   // FK

        [StringLength(100)]
        public string? City { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Navigation
        public Region? Region { get; set; }

        public ICollection<Order>? Orders { get; set; }
    }
}


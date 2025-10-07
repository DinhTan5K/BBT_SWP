using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("Region")]
    public class Region
    {
        [Key]
        public int RegionID { get; set; }

        [Required, StringLength(50)]
        public string RegionName { get; set; } = string.Empty;

        // Navigation
        public ICollection<Branch>? Branches { get; set; }
    }
}

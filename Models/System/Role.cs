using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace start.Models
{
    [Table("Role")]
    public class Role
    {
        [Key] [StringLength(2)]
        public string RoleID { get; set; } = null!;
        [Required] [StringLength(50)]
        public string RoleName { get; set; } = null!;
        public ICollection<Employee>? Employees { get; set; }
    }
}

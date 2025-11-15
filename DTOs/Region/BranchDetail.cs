using System.Collections.Generic;

namespace start.DTOs
{
    public class BranchDetail
    {
        public int BranchID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        // branch manager info (from BranchManage -> Employee)
        public string? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerPhone { get; set; }


        // coordinates
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // employees list (role EM)
        public List<EmployeeBranchDetail> Employees { get; set; } = new List<EmployeeBranchDetail>();
    }
}

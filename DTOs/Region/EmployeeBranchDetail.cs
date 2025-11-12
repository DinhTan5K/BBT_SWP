using System;

namespace start.DTOs
{
    public class EmployeeBranchDetail
    {
        public string EmployeeID { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime HireDate { get; set; }

        public string RoleId { get; set; } = string.Empty;

        
    }
}

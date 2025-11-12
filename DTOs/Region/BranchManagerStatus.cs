namespace start.DTOs
{
    public class BranchManagerStatus
    {
        public string EmployeeID { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }      // nếu DB có, hoặc null
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
    }
}

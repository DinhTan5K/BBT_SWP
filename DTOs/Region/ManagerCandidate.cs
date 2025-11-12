namespace start.DTOs
{
    public class ManagerCandidate
    {
        public string EmployeeID { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; } // 1 = đang làm, 0 = đang nghỉ
        public int? BranchID { get; set; } // chi nhánh hiện tại (nếu có)
        public string? BranchName { get; set; }
    }
}

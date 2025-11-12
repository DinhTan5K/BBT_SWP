namespace start.DTOs
{
    public class BranchStatus
    {
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public string? ManagerName { get; set; }    // có thể null nếu chưa có quản lý

        public string? PhoneNumber { get; set; }    // có thể null nếu chưa có số điện thoại
    }
}

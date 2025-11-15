using System;

namespace start.DTOs;

public class ChangeBranchManager
{
     public int BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public string? CurrentManagerId { get; set; }
    public string? CurrentManagerName { get; set; }
    public List<ManagerCandidate> Candidates { get; set; } = new();
}

using start.Models;
using start.Models.ViewModels;
public interface IDayOffService
{
    Task<int> CreateOneDayAsync(DayOffOneDayVm vm);
    Task<List<DayOffListItemVm>> GetMyAsync(string employeeId);

Task<List<DayOffManagerVm>> GetPendingByBranchAsync(int branchId);
    Task<(bool success, string message)> UpdateStatusAsync(int requestId, int branchId, string newStatus);
}
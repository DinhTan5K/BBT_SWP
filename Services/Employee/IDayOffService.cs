using start.Models;
using start.Models.ViewModels;
public interface IDayOffService
{
    Task<int> CreateOneDayAsync(DayOffOneDayVm vm);
    Task<List<DayOffListItemVm>> GetMyAsync(string employeeId);

}
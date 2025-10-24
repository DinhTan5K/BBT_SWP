using start.Models;
using start.Models.ViewModels;
public interface IScheduleService
{
    MonthScheduleDto GetMonthSchedule(string employeeId, int month, int year);
}
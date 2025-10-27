using start.Models;
using start.Models.ViewModels;
public interface IPayrollService
{
    Task<MonthlySalaryVm?> GetMonthlySalaryAsync(string employeeId, int year, int month);
}
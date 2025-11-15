using start.Models;
using start.Models.ViewModels;
using start.DTOs;
public interface IPayrollService
{
    Task<MonthlySalaryVm?> GetMonthlySalaryAsync(string employeeId, int year, int month);
    Task<(bool success, string message)> CalculateAndFinalizeSalaryAsync(string employeeId, int year, int month, int branchId);

    Task<ManagerSalaryDetailVm?> GetManagerMonthlySalaryAsync(string managerId, int year, int month);
    
}
using start.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace start.Services
{
    public interface IEmployeeManagementService
    {
        Task<List<Employee>> GetAllEmployeesByBranchAsync(int branchId);
        Task<(bool Success, string? ErrorMessage)> HideEmployeeAsync(string employeeId, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> RestoreEmployeeAsync(string employeeId, int managerBranchId);
        Task<IEnumerable<Role>> GetSelectableRolesAsync();
       Task<(bool success, string message)> SubmitAddEmployeeRequestAsync(EmployeeBranchRequest request);
        Task<Employee?> GetEmployeeByIdAsync(string id);
        Task<(bool Success, Dictionary<string, string> Errors)> UpdateEmployeeAsync(Employee empFromForm, int managerBranchId);
    }
}
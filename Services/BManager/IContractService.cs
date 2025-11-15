using start.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace start.Services
{
    public interface IContractService
    {
    Task<List<Contract>> GetContractsByEmployeeIdAsync(string employeeId, int managerBranchId);
    Task<(bool success, string message)> CreateContractAsync(Contract contract, int managerBranchId);
    Task<Contract?> GetActiveHourlyContractOnDateAsync(string employeeId, DateTime date);

    Task<(bool success, string message)> CancelContractAsync(int contractId, int managerBranchId);
}
    
}
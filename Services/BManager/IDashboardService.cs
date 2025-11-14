using start.Models;
using System.Threading.Tasks;
using start.DTOs;

namespace start.Services
{
    public interface IDashboardService
    {
        Task<BranchManagerDashboardViewModel> GetDashboardSummaryAsync(int branchId);
        Task<(bool Success, string ErrorMessage)> UpdateBranchTargetsAsync(int branchId, int targetOrders, decimal targetRevenue);
        Task<List<WeeklySummary>> GetWeeklyEmployeeSummaryAsync(int branchId);
    }
}
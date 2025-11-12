using start.Models;
using start.Models.ViewModels;

namespace start.Services
{
    public interface IMarketingKPIService
    {
        /// <summary>
        /// Tính KPI cho Marketing employee trong tháng
        /// </summary>
        Task<MarketingKPIVm?> CalculateKPIAsync(string employeeId, int year, int month);

        /// <summary>
        /// Lấy KPI đã tính sẵn từ database
        /// </summary>
        Task<MarketingKPIVm?> GetKPIAsync(string employeeId, int year, int month);

        /// <summary>
        /// Tính và lưu KPI vào database
        /// </summary>
        Task<MarketingKPIVm?> CalculateAndSaveKPIAsync(string employeeId, int year, int month);

        /// <summary>
        /// Tính bonus dựa trên KPI score
        /// </summary>
        decimal CalculateKPIBonus(decimal kpiScore, decimal baseSalary);
    }
}


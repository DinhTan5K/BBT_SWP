using start.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace start.Services
{
    public interface IReportService
    {
        Task<List<SalaryReport>> GetSalaryReportAsync(string? name, int month, int year, int branchId); 

        Task<List<RevenueReport>> GetRevenueReportAsync(int branchId, DateTime startDate, DateTime endDate);
        Task<List<Order>> GetOrdersByDateAsync(int branchId, DateTime date);
        Task<List<WorkSchedule>> GetAllWorkSchedulesForMonthAsync(int month, int year, int branchId);
        Task<List<DetailedShiftReport>> GetDetailedWorkSchedulesAsync(string employeeId, int month, int year, int branchId);
}
    
}
using start.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace start.Services
{

    public interface IScheduleForManagerService
    {
        Task<List<WorkSchedule>> GetWorkScheduleAsync(int branchId, DateTime? startDate, DateTime? endDate);
        Task<List<Employee>> GetActiveEmployeeAsync(int branchID);
        Task<(bool Success, string? ErrorMessage)> CreateScheduleAsync(WorkSchedule schedule);
        Task<(bool Success, string? ErrorMessage)> ManagerCreateScheduleAsync(WorkSchedule schedule, int managerBranchId);
        Task<WorkSchedule?> GetScheduleByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> UpdateScheduleAsync(WorkSchedule schedule, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> HideScheduleAsync(int id, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> RestoreScheduleAsync(int id, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> ApproveScheduleAsync(int scheduleId, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> RejectScheduleAsync(int scheduleId, int managerBranchId);
        Task<List<WorkSchedule>> GetScheduleDetailsForDateAsync(int branchId, DateTime date);
        
        // Các hàm phụ
       
        Task<List<WorkSchedule>> GetSchedulesForCalendarAsync(int branchId, DateTime start, DateTime end);
        Task<List<WorkSchedule>> GetSchedulesForEmployeeAsync(string employeeId, DateTime start, DateTime end);
    }
}
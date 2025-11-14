using start.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public interface IRegisterScheduleService
    {
        
        Task<List<WorkSchedule>> GetMySchedulesAsync(string employeeId, DateTime start, DateTime end);

      
        Task<(bool success, string message)> RegisterSelfForShiftAsync(string employeeId, ScheduleRequest request);

      
        Task<(bool success, string message)> CancelShiftAsync(string employeeId, int scheduleId);
    }
}
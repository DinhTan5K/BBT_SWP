using Microsoft.AspNetCore.Http;
using start.Models;

namespace start.Services
{
    public interface IAttendanceService
    {
        Task<(bool success, string message, Attendance? attendance)> CheckInAsync(
            string employeeId, 
            int? workScheduleId,
            string capturedImageBase64);

        Task<(bool success, string message, Attendance? attendance)> CheckOutAsync(
            string employeeId,
            string capturedImageBase64);

        Task<Attendance?> GetTodayCheckInAsync(string employeeId);
        Task<List<Attendance>> GetAttendanceHistoryAsync(string employeeId, DateTime? fromDate, DateTime? toDate);
        Task<bool> UploadFaceImageAsync(string employeeId, IFormFile faceImage);
        Task<bool> CompareFacesAsync(Employee employee, string capturedImageBase64);

    }
}





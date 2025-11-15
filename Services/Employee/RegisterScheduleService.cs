using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class RegisterScheduleService : IRegisterScheduleService
    {
        private readonly ApplicationDbContext _context;

        public RegisterScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy lịch làm việc của CÁ NHÂN nhân viên
        public async Task<List<WorkSchedule>> GetMySchedulesAsync(string employeeId, DateTime start, DateTime end)
        {
            return await _context.WorkSchedules
                .Where(ws => ws.EmployeeID == employeeId &&
                             ws.IsActive && 
                             ws.Date >= start && 
                             ws.Date <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        // 2. Logic Đăng ký ca (đã di chuyển từ Controller và BManagerService)
        public async Task<(bool success, string message)> RegisterSelfForShiftAsync(string employeeId, ScheduleRequest request)
        {
            var workDateOnly = request.WorkDate.Date;

            // Kiểm tra logic thời gian (từ Controller)
            var today = DateTime.Today;
            if (workDateOnly < today)
            {
                return (false, "Không thể đăng ký lịch cho ngày trong quá khứ.");
            }
            
            // Logic kiểm tra "cửa sổ đăng ký" (từ Controller)
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                return (false, "Đã hết hạn đăng ký tuần sau. Vui lòng đợi đến Thứ Hai.");
            }
            int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilNextMonday == 0) daysUntilNextMonday = 7; 
            var startOfNextWeek = today.AddDays(daysUntilNextMonday);
            var endOfNextWeek = startOfNextWeek.AddDays(6);

            if (workDateOnly < startOfNextWeek || workDateOnly > endOfNextWeek)
            {
                return (false, $"Bạn chỉ có thể đăng ký lịch cho tuần sau (từ {startOfNextWeek:dd/MM} đến {endOfNextWeek:dd/MM}).");
            }

            // Kiểm tra trùng lặp (logic từ BManagerService.CreateScheduleAsync)
            bool scheduleExists = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeID == employeeId && 
                               ws.Date == workDateOnly && 
                               ws.Shift == request.ShiftType);
            if (scheduleExists)
            {
                return (false, "Bạn đã đăng ký ca này rồi.");
            }
            
            // Lấy thông tin nhân viên để xác định BranchId
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || !employee.BranchID.HasValue)
            {
                return (false, "Không tìm thấy thông tin chi nhánh của nhân viên.");
            }
            
            // Tạo lịch mới
            var schedule = new WorkSchedule
            {
                EmployeeID = employeeId,
                Date = workDateOnly,
                Shift = request.ShiftType,
                Status = "Chưa duyệt", // Mọi đăng ký mới đều là "Chưa duyệt"
                IsActive = true,
                BranchId = employee.BranchID.Value // Gán BranchId từ thông tin nhân viên
                // CheckInTime = null,
                // CheckOutTime = null
            };

            _context.WorkSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            
            return (true, "Đăng ký ca thành công! Vui lòng chờ duyệt.");
        }

        // 3. Logic Hủy ca (đã di chuyển từ BManagerService.RejectScheduleAsync)
        public async Task<(bool success, string message)> CancelShiftAsync(string employeeId, int scheduleId)
        {
            var schedule = await _context.WorkSchedules
                .FirstOrDefaultAsync(ws => ws.WorkScheduleID == scheduleId);

            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");
            
            // Bảo mật: Chỉ chủ nhân của lịch mới được hủy
            if (schedule.EmployeeID != employeeId)
            {
                return (false, "Bạn không có quyền hủy lịch này.");
            }
            
            if (schedule.Status != "Chưa duyệt")
            {
                return (false, "Bạn chỉ có thể hủy các ca đang Chờ duyệt.");
            }

            _context.WorkSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return (true, "Đã hủy ca đăng ký.");
        }
    }
}
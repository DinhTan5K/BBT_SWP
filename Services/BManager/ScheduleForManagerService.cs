using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class ScheduleForManagerService : IScheduleForManagerService
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] EmployeeAndShiftLeadRoles = { "EM", "SL", "SH" };

        public ScheduleForManagerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkSchedule>> GetWorkScheduleAsync(int branchId, DateTime? startDate, DateTime? endDate)
        {
            // Sửa lỗi: Lấy danh sách ID nhân viên bao gồm cả Shipper
            var employeeIdsInBranch = await _context.Employees
                .Where(e => e.BranchID == branchId && EmployeeAndShiftLeadRoles.Contains(e.RoleID))
                .Select(e => e.EmployeeID)
                .ToListAsync();

            if (!employeeIdsInBranch.Any())
            {
                return new List<WorkSchedule>();
            }

            var query = _context.WorkSchedules
                    .Include(ws => ws.Employee)
                    // Sửa lỗi: Lọc các lịch làm việc theo danh sách ID nhân viên đã lấy ở trên
                    .Where(ws => employeeIdsInBranch.Contains(ws.EmployeeID))
                    .AsNoTracking()
                    .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(ws => ws.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ws => ws.Date <= endDate.Value);
            }

            return await query.OrderBy(ws => ws.Date).ToListAsync();
        }

        public async Task<List<Employee>> GetActiveEmployeeAsync(int branchID)
        {
            return await _context.Employees
                .Where(e => e.IsActive && EmployeeAndShiftLeadRoles.Contains(e.RoleID) && e.BranchID == branchID)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateScheduleAsync(WorkSchedule schedule)
        {
            if (schedule.Date < DateTime.Today)
            {
                return (false, "❌ Ngày làm việc không được nhỏ hơn ngày hôm nay.");
            }

            bool scheduleExists = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeID == schedule.EmployeeID &&
                               ws.Date == schedule.Date.Date &&
                               ws.Shift == schedule.Shift);

            if (scheduleExists)
            {
                return (false, "❌ Bạn đã đăng ký ca này rồi.");
            }

            schedule.CheckInTime = null;
            schedule.CheckOutTime = null;
            schedule.IsActive = true;
            schedule.Status = "Chưa duyệt";

            _context.WorkSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return (true, "Đăng ký thành công! Vui lòng chờ quản lý duyệt.");
        }
        public async Task<(bool Success, string? ErrorMessage)> ManagerCreateScheduleAsync(WorkSchedule schedule, int managerBranchId)
        {
            // 1. Kiểm tra nhân viên có thuộc chi nhánh của quản lý hay không
            var employee = await _context.Employees.FindAsync(schedule.EmployeeID);
            if (employee == null || employee.BranchID != managerBranchId)
            {
                return (false, "Nhân viên không thuộc chi nhánh của bạn.");
            }

            // 2. Không cho tạo lịch trong quá khứ
            if (schedule.Date.Date < DateTime.Today.Date)
            {
                return (false, "Không thể xếp lịch cho ngày trong quá khứ.");
            }

            //  3. CHỈ kiểm tra trùng lịch theo NHÂN VIÊN
            // Một nhân viên chỉ được có 1 lịch duyệt trong cùng ngày và cùng ca
            bool alreadyApproved = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeID == schedule.EmployeeID &&
                                ws.Date == schedule.Date.Date &&
                                ws.Shift == schedule.Shift &&
                                ws.Status == "Đã duyệt");

            if (alreadyApproved)
            {
                return (false, "Nhân viên này đã có ca làm được duyệt trong ngày này.");
            }

            //  4. Cho phép tạo cho nhân viên khác cùng ngày/ca (không cần kiểm tra branch-level trùng)
            // Tự động xoá các yêu cầu chờ duyệt trùng (nếu có)
            var pendingRequests = await _context.WorkSchedules
                .Where(ws => ws.EmployeeID == schedule.EmployeeID &&
                             ws.Date == schedule.Date.Date &&
                             ws.Shift == schedule.Shift &&
                             ws.Status == "Chưa duyệt")
                .ToListAsync();

            if (pendingRequests.Any())
            {
                _context.WorkSchedules.RemoveRange(pendingRequests);
            }

            //  5. Gán thông tin mặc định và lưu
            schedule.CheckInTime = null;
            schedule.CheckOutTime = null;
            schedule.IsActive = true;
            schedule.Status = "Đã duyệt"; // Quản lý tạo là duyệt luôn

            _context.WorkSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return (true, "Xếp lịch thành công.");
        }

        public async Task<WorkSchedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.WorkSchedules.FindAsync(id);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateScheduleAsync(WorkSchedule schedule, int managerBranchId)
        {
            var existingSchedule = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(s => s.WorkScheduleID == schedule.WorkScheduleID);

            if (existingSchedule == null)
                return (false, "Không tìm thấy lịch làm việc.");

            if (existingSchedule.Employee?.BranchID != managerBranchId)
                return (false, "Bạn không có quyền sửa lịch làm việc này.");

            if (schedule.Date.Date < DateTime.Today.Date)
                return (false, "Ngày làm việc không được là ngày trong quá khứ.");

            bool scheduleExists = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeID == schedule.EmployeeID &&
                               ws.Date == schedule.Date &&
                               ws.Shift == schedule.Shift &&
                               ws.WorkScheduleID != schedule.WorkScheduleID);

            if (scheduleExists)
                return (false, "Nhân viên này đã có ca làm vào ngày này.");

            existingSchedule.EmployeeID = schedule.EmployeeID;
            existingSchedule.Date = schedule.Date;
            existingSchedule.Shift = schedule.Shift;
            existingSchedule.Status = schedule.Status ?? existingSchedule.Status;
            existingSchedule.IsActive = schedule.IsActive;


           try
    {
        _context.WorkSchedules.Update(existingSchedule);
        await _context.SaveChangesAsync();
        return (true, null);
    }
    catch (Exception ex)
    {
        
        return (false, $"Lỗi hệ thống khi cập nhật: {ex.Message}");
    }
        }

        public async Task<(bool Success, string? ErrorMessage)> HideScheduleAsync(int id, int managerBranchId)
        {
            var schedule = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.WorkScheduleID == id);

            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");

            if (schedule.Employee?.BranchID != managerBranchId)
            {
                return (false, "Bạn không có quyền ẩn lịch làm việc này.");
            }

            schedule.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RestoreScheduleAsync(int id, int managerBranchId)
        {
            var schedule = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.WorkScheduleID == id);

            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");

            if (schedule.Employee?.BranchID != managerBranchId)
            {
                return (false, "Bạn không có quyền khôi phục lịch làm việc này.");
            }

            schedule.IsActive = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string message)> CheckInAsync(int workScheduleID)
        {
            var schedule = await _context.WorkSchedules.FindAsync(workScheduleID);
            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");
            if (schedule.CheckInTime.HasValue) return (false, "Đã check-in rồi.");

            schedule.CheckInTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Check-in thành công.");
        }

        public async Task<(bool success, string message)> CheckOutAsync(int workScheduleID)
        {
            var schedule = await _context.WorkSchedules.FindAsync(workScheduleID);
            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");
            if (!schedule.CheckInTime.HasValue) return (false, "Chưa check-in.");
            if (schedule.CheckOutTime.HasValue) return (false, "Đã check-out rồi.");

            schedule.CheckOutTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Check-out thành công.");
        }

        public async Task<(bool Success, string? ErrorMessage)> ApproveScheduleAsync(int scheduleId, int managerBranchId)
        {
            var schedule = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.WorkScheduleID == scheduleId);

            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");

            if (schedule.Employee?.BranchID != managerBranchId)
            {
                return (false, "Bạn không có quyền duyệt lịch này.");
            }

            bool scheduleExists = await _context.WorkSchedules
                .AnyAsync(ws => ws.EmployeeID == schedule.EmployeeID &&
                               ws.Date == schedule.Date &&
                               ws.Status == "Đã duyệt" &&
                               ws.WorkScheduleID != scheduleId);
            if (scheduleExists)
            {
                return (false, "Nhân viên này đã có ca làm được duyệt vào ngày này.");
            }

            schedule.Status = "Đã duyệt";
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RejectScheduleAsync(int scheduleId, int managerBranchId)
        {
            var schedule = await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .FirstOrDefaultAsync(ws => ws.WorkScheduleID == scheduleId);

            if (schedule == null) return (false, "Không tìm thấy lịch làm việc.");

            if (schedule.Employee?.BranchID != managerBranchId)
            {
                return (false, "Bạn không có quyền xóa lịch này.");
            }

            if (schedule.Status != "Chưa duyệt")
            {
                return (false, "Bạn chỉ có thể từ chối các ca đang Chờ duyệt.");
            }

            _context.WorkSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<WorkSchedule>> GetSchedulesForCalendarAsync(int branchId, DateTime start, DateTime end)
        {
            return await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .Where(ws => ws.Employee.BranchID == branchId && ws.IsActive && ws.Date >= start && ws.Date <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetSchedulesForEmployeeAsync(string employeeId, DateTime start, DateTime end)
        {
            return await _context.WorkSchedules
                .Where(ws => ws.EmployeeID == employeeId &&
                             ws.IsActive &&
                             ws.Date >= start &&
                             ws.Date <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WorkSchedule>> GetScheduleDetailsForDateAsync(int branchId, DateTime date)
        {
            var targetDate = date.Date;

            var employeeIdsInBranch = await _context.Employees
                .Where(e => e.BranchID == branchId && EmployeeAndShiftLeadRoles.Contains(e.RoleID))
                .Select(e => e.EmployeeID)
                .ToListAsync();

            if (!employeeIdsInBranch.Any())
            {
                return new List<WorkSchedule>();
            }

            return await _context.WorkSchedules
                .Include(ws => ws.Employee)
                .Where(ws =>
                    employeeIdsInBranch.Contains(ws.EmployeeID) &&
                    ws.Date == targetDate
                )
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class EmployeeManagementService : IEmployeeManagementService
    {
        private readonly ApplicationDbContext _context;
        // Sửa lại mã vai trò của Shipper từ "SP" thành "SP" để khớp với database
        private static readonly string[] EmployeeAndShiftLeadRoles = { "EM", "SL", "SP" };

        public EmployeeManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetAllEmployeesByBranchAsync(int branchId)
        {
            return await _context.Employees
                 .Where(e => e.BranchID == branchId && EmployeeAndShiftLeadRoles.Contains(e.RoleID))
                 .AsNoTracking()
                 .ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> HideEmployeeAsync(string employeeId, int managerBranchId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
            if (employee == null) return (false, "Employee not found.");

            if (employee.BranchID != managerBranchId)
            {
                return (false, "You do not have permission to modify this employee.");
            }

            employee.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RestoreEmployeeAsync(string employeeId, int managerBranchId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
            if (employee == null) return (false, "Employee not found.");

            if (employee.BranchID != managerBranchId)
            {
                return (false, "You do not have permission to modify this employee.");
            }

            employee.IsActive = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<IEnumerable<Role>> GetSelectableRolesAsync()
        {
            System.Diagnostics.Debug.WriteLine("🔥 GetSelectableRolesAsync CALLED!");
            System.Diagnostics.Debug.WriteLine(
                "ROLES FILTER: " + string.Join(",", EmployeeAndShiftLeadRoles)
            );

            var roles = await _context.Roles
                 .Where(r => EmployeeAndShiftLeadRoles.Contains(r.RoleID))
                 .AsNoTracking()
                 .ToListAsync();

            return roles;
        }

        // public async Task<(Employee Employee, string? ErrorMessage)> CreateEmployeeAsync(Employee emp, int managerBranchId)
        // {
        //     if (!string.IsNullOrEmpty(emp.PhoneNumber) && await _context.Employees.AnyAsync(e => e.PhoneNumber == emp.PhoneNumber))
        //     {
        //         return (emp, "⚠️ This phone number is already in use.");
        //     }

        //     if (!string.IsNullOrEmpty(emp.Email) && await _context.Employees.AnyAsync(e => e.Email == emp.Email))
        //     {
        //         return (emp, "⚠️ This email is already in use.");
        //     }

        //     if (string.IsNullOrEmpty(emp.RoleID) || !EmployeeAndShiftLeadRoles.Contains(emp.RoleID))
        //     {
        //         return (emp, "⚠️ Vai trò được chọn không hợp lệ.");
        //     }

        //     var lastEmpId = await _context.Employees
        //                              .Where(e => e.EmployeeID.StartsWith("EM"))
        //                              .Select(e => e.EmployeeID)
        //                              .OrderByDescending(id => id.Length)
        //                              .ThenByDescending(id => id)
        //                              .FirstOrDefaultAsync();

        //     int nextNumber = 1;
        //     if (lastEmpId != null && int.TryParse(lastEmpId.Substring(2), out int lastNumber))
        //     {
        //         nextNumber = lastNumber + 1;
        //     }

        //     emp.EmployeeID = $"EM{nextNumber:D2}";
        //     emp.BranchID = managerBranchId;
        //     emp.IsActive = true;
        //     emp.HireDate = DateTime.Now;

        //     _context.Employees.Add(emp);
        //     await _context.SaveChangesAsync();

        //     return (emp, null);
        // }

        public async Task<Employee?> GetEmployeeByIdAsync(string id)
        {
            return await _context.Employees
                 .AsNoTracking()
                 .FirstOrDefaultAsync(e => e.EmployeeID == id);
        }

        public async Task<(bool success, string message)> SubmitAddEmployeeRequestAsync(EmployeeBranchRequest request)
        {
            // 1. Validation (Kiểm tra trùng lặp với nhân viên đang hoạt động)
            // Dùng request.Email/request.PhoneNumber
            if (await _context.Employees.AnyAsync(e => e.Email == request.Email!))
                return (false, "⚠️ Email này đã tồn tại trong hệ thống.");

            if (await _context.Employees.AnyAsync(e => e.PhoneNumber == request.PhoneNumber!))
                return (false, "⚠️ Số điện thoại này đã tồn tại trong hệ thống.");

            // 2. Kiểm tra trùng lặp với YÊU CẦU đang chờ duyệt
            bool dupeRequest = await _context.EmployeeBranchRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .AnyAsync(r => r.Email == request.Email || r.PhoneNumber == request.PhoneNumber);

            if (dupeRequest)
                return (false, "⚠️ Đã có yêu cầu thêm nhân viên với Email/SĐT này đang chờ duyệt.");

            // 3. Gán thời gian (vì bạn đã xóa khỏi ModelState)
            request.RequestedAt = DateTime.Now;

            try
            {
                // ⭐️ Thêm Entity trực tiếp (vì nó đã là Model Entity)
                _context.EmployeeBranchRequests.Add(request);
                await _context.SaveChangesAsync();

                return (true, $"Yêu cầu thêm nhân viên '{request.FullName}' đã được gửi đi. Vui lòng chờ Admin duyệt.");
            }
            catch (Exception)
            {
                // Ghi log lỗi chi tiết
                return (false, "Lỗi hệ thống khi gửi yêu cầu.");
            }
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> UpdateEmployeeAsync(Employee empFromForm, int managerBranchId)
        {
            var errors = new Dictionary<string, string>();
            var existingEmp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == empFromForm.EmployeeID);

            if (existingEmp == null)
            {
                errors.Add("", "Employee not found.");
                return (false, errors);
            }

            if (existingEmp.BranchID != managerBranchId)
            {
                errors.Add("", "You do not have permission to edit this employee.");
                return (false, errors);
            }

            var newRoleId = empFromForm.RoleID;
            if (string.IsNullOrEmpty(newRoleId) || !EmployeeAndShiftLeadRoles.Contains(newRoleId))
            {
                errors.Add("RoleID", "Vai trò được chọn không hợp lệ.");
                return (false, errors);
            }

            if (await _context.Employees.AnyAsync(e => e.Email == empFromForm.Email && e.EmployeeID != empFromForm.EmployeeID))
                errors.Add("Email", "⚠️ This email is already in use by another employee.");

            if (await _context.Employees.AnyAsync(e => e.PhoneNumber == empFromForm.PhoneNumber && e.EmployeeID != empFromForm.EmployeeID))
                errors.Add("Phone", "⚠️ This phone number is already in use by another employee.");

            if (errors.Any()) return (false, errors);

            existingEmp.FullName = empFromForm.FullName;
            existingEmp.DateOfBirth = empFromForm.DateOfBirth;
            existingEmp.PhoneNumber = empFromForm.PhoneNumber;
            existingEmp.Email = empFromForm.Email;
            existingEmp.City = empFromForm.City;
            existingEmp.RoleID = newRoleId;

            await _context.SaveChangesAsync();
            return (true, new Dictionary<string, string>());
        }
    }
}
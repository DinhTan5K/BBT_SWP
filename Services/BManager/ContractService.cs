using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class ContractService : IContractService
    {
        private readonly ApplicationDbContext _context;

        public ContractService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trong Services/ContractService.cs

        public async Task<(bool success, string message)> CancelContractAsync(int contractId, int managerBranchId)
        {
            // Sửa lỗi truy vấn: Tải dữ liệu trước
            var contract = await _context.Contracts
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null || contract.Employee == null || contract.Employee.BranchID != managerBranchId)
            {
                return (false, "Contract not found or you don't have permission.");
            }

            // Ràng buộc: chỉ hủy khi còn hiệu lực
            if (!contract.IsActive)
            {
                return (false, "Contract is not active and cannot be cancelled.");
            }

            // --- LOGIC TRẠNG THÁI THEO YÊU CẦU: HetHan ---
            contract.Status = Contract.Statuses.HetHan;

            // Đặt EndDate là hôm nay để chấm dứt hiệu lực ngay lập tức
            if (!contract.EndDate.HasValue || contract.EndDate.Value > DateTime.Today)
            {
                contract.EndDate = DateTime.Today;
            }

            contract.UpdatedAt = DateTime.Now;

            try
            {
                // EF Core tự động nhận diện thay đổi trạng thái (HetHan)
                await _context.SaveChangesAsync();
                return (true, "Contract cancelled successfully.");
            }
            catch (DbUpdateException)
            {
                // Vẫn nên kiểm tra lỗi ràng buộc DB (ví dụ: ngày tháng)
                return (false, "Database error occurred while cancelling the contract.");
            }
            catch (Exception)
            {
                return (false, "An unexpected error occurred.");
            }
        }




        public async Task<List<Contract>> GetContractsByEmployeeIdAsync(string employeeId, int managerBranchId)
        {
            var employeeExistsInBranch = await _context.Employees
                .AnyAsync(e => e.EmployeeID == employeeId && e.BranchID == managerBranchId);

            if (!employeeExistsInBranch)
            {
                return new List<Contract>();
            }

            return await _context.Contracts
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }



        public async Task<(bool success, string message)> CreateContractAsync(Contract contract, int managerBranchId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == contract.EmployeeId && e.BranchID == managerBranchId);

            if (employee == null)
            {
                return (false, "Employee not found in your branch or you don't have permission.");
            }

            if (contract.EndDate.HasValue && contract.StartDate > contract.EndDate.Value)
            {
                return (false, "End date cannot be earlier than the start date.");
            }

            if (await _context.Contracts.AnyAsync(c => c.ContractNumber == contract.ContractNumber))
            {
                return (false, "Contract number already exists.");
            }


            if (contract.EndDate.HasValue && contract.EndDate.Value < DateTime.Today)
            {
                contract.Status = Contract.Statuses.HetHan;
            }
            else
            {

                contract.Status = Contract.Statuses.HieuLuc;
            }

            contract.CreatedAt = DateTime.Now;
            contract.UpdatedAt = null;

            try
            {
                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();
                return (true, "Contract created successfully.");
            }
            catch (DbUpdateException)
            {

                return (false, "Database error occurred while saving the contract.");
            }
            catch (Exception)
            {

                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<Contract?> GetActiveHourlyContractOnDateAsync(string employeeId, DateTime date)
        {
            var targetDate = date.Date;

            return await _context.Contracts
                .Where(c => c.EmployeeId == employeeId
                            && c.Status == Contract.Statuses.HieuLuc
                            && c.PaymentType == Contract.PaymentTypes.Gio
                            && c.StartDate <= targetDate
                            && (c.EndDate == null || c.EndDate >= targetDate)
                      )
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

        }
       

    }
}

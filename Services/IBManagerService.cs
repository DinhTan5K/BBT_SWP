using start.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;


namespace start.Services
{

    public interface IBManagerService
    {
        // Employee
        Task<IEnumerable<Role>> GetSelectableRolesAsync();
        Task<List<Employee>> GetAllEmployeesByBranchAsync(int branchId); 
        Task<(Employee Employee, string? ErrorMessage)> CreateEmployeeAsync(Employee emp, int managerBranchId);
        Task<Employee?> GetEmployeeByIdAsync(string id);
        Task<(bool Success, Dictionary<string, string> Errors)> UpdateEmployeeAsync(Employee empFromForm, int managerBranchId);

        Task<(bool Success, string? ErrorMessage)> HideEmployeeAsync(string employeeId, int managerBranchId);
        Task<(bool Success, string? ErrorMessage)> RestoreEmployeeAsync(string employeeId, int managerBranchId);
        // Product
       Task<List<Product>> GetAllProductsWithSizesAsync();
        Task<List<ProductCategory>> GetProductCategoriesAsync(); 
        Task<(bool Success, Dictionary<string, string> Errors)> CreateProductAsync(Product product);
        Task<(Product? Product, List<ProductCategory> ProductCategories)> GetProductForEditAsync(int id);
        Task<(bool Success, Dictionary<string, string> Errors)> UpdateProductAsync(Product product);
        Task HideProductAsync(int id);
        Task RestoreProductAsync(int id);


        // Work Schedule
        Task<List<WorkSchedule>> GetWorkSchedulesAsync(int branchId, DateTime? startDate, DateTime? endDate);    
        Task<List<Employee>> GetActiveEmployeesAsync(int branchID); 
        Task<(bool Success, string? ErrorMessage)> CreateScheduleAsync(WorkSchedule schedule);
        Task<WorkSchedule?> GetScheduleByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> UpdateScheduleAsync(WorkSchedule schedule);
        Task HideScheduleAsync(int id);
        Task RestoreScheduleAsync(int id);





        // Salary Report
        Task<List<SalaryReport>> GetSalaryReportAsync(string? name, int month, int year, double ratePerHour, double hoursPerShift);

       Task<List<RevenueReport>> GetRevenueReportAsync(int branchId, DateTime startDate, DateTime endDate);



        // Task<(bool Success, string? ErrorMessage)> CreateNewsAsync(CreateNews viewModel, string webRootPath);

        //Dashborad
        Task<BranchManagerDashboardViewModel> GetDashboardSummaryAsync(int branchId);

        //Contract 

        Task<List<Contract>> GetContractsByEmployeeIdAsync(string employeeId, int managerBranchId);
        Task<(bool success, string message)> CreateContractAsync(Contract contract, int managerBranchId);


    }



}
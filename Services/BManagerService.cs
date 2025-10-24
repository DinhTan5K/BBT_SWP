

using start.Data;
using start.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace start.Services
{
    public class BManagerService : IBManagerService
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] EmployeeAndShiftLeadRoles = { "EM", "SL" };
        private const string OrderStatusCompleted = "Đã giao";
        // private const double HoursPerShift = 6;
        // private const double RatePerHour = 25000;

        public BManagerService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<BranchManagerDashboardViewModel> GetDashboardSummaryAsync(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch == null)
            {
                return new BranchManagerDashboardViewModel { BranchName = "Không tìm thấy chi nhánh" };
            }

            var today = DateTime.Today;
var employeeCount = await _context.Employees
                .CountAsync(e => e.BranchID == branchId && e.IsActive && EmployeeAndShiftLeadRoles.Contains(e.RoleID));

            var todayOrders = await _context.Orders
                
                .Where(o => o.BranchID == branchId && o.CreatedAt.Date == today && o.Status == OrderStatusCompleted)
                .ToListAsync();
            var todayOrdersCount = todayOrders.Count;
            var todayRevenue = todayOrders.Sum(o => o.Total);

            // Giả sử tên người quản lý có thể lấy từ db, tạm thời để trống
            return new BranchManagerDashboardViewModel
            {
                BranchName = branch.Name,
                EmployeeCount = employeeCount,
                TodayOrdersCount = todayOrdersCount,
                TodayRevenue = todayRevenue
            };
        }

        // ------------------ EMPLOYEE METHODS ------------------

        public async Task<List<Employee>> GetAllEmployeesByBranchAsync(int branchId){
            //var rolesToShow = new[] { "EM", "SL" };

           return await _context.Employees
                .Where(e => e.BranchID == branchId && EmployeeAndShiftLeadRoles.Contains(e.RoleID))
                .AsNoTracking() 
                .ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> HideEmployeeAsync(string employeeId, int managerBranchId)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == employeeId);

            if (employee == null)
            {
                return (false, "Employee not found.");
            }

            // ADDED: Security check to ensure the employee belongs to the manager's branch.
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

            if (employee == null)
            {
                return (false, "Employee not found.");
            }

            // ADDED: Security check to ensure the employee belongs to the manager's branch.
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
           return await _context.Roles
                .Where(r => EmployeeAndShiftLeadRoles.Contains(r.RoleID))
                .AsNoTracking() // THÊM: Cải thiện hiệu suất
                .ToListAsync();
        }

        public async Task<(Employee Employee, string? ErrorMessage)> CreateEmployeeAsync(Employee emp, int managerBranchId)
        {
            if (!string.IsNullOrEmpty(emp.PhoneNumber) && await _context.Employees.AnyAsync(e => e.PhoneNumber == emp.PhoneNumber))
            {
                return (emp, "⚠️ This phone number is already in use.");
            }

            if (!string.IsNullOrEmpty(emp.Email) && await _context.Employees.AnyAsync(e => e.Email == emp.Email))
            {
                return (emp, "⚠️ This email is already in use.");
            }

            if (string.IsNullOrEmpty(emp.RoleID) || !EmployeeAndShiftLeadRoles.Contains(emp.RoleID))
            {
                return (emp, "⚠️ Vai trò được chọn không hợp lệ.");
            }

            var lastEmpId = await _context.Employees
                                  .Where(e => e.EmployeeID.StartsWith("EM"))
                                  .Select(e => e.EmployeeID) // Chỉ lấy ID
                                  .OrderByDescending(id => id.Length) // Sắp xếp theo độ dài (EM100 > EM99)
                                  .ThenByDescending(id => id) // Sau đó sắp xếp theo chuỗi
                                  .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEmpId != null && int.TryParse(lastEmpId.Substring(2), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            emp.EmployeeID = $"EM{nextNumber:D2}";
            emp.BranchID = managerBranchId;
            emp.IsActive = true;
            emp.HireDate = DateTime.Now;

            _context.Employees.Add(emp);
            await _context.SaveChangesAsync();

            return (emp, null);
        }

        public async Task<Employee?> GetEmployeeByIdAsync(string id)
        {
            return await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == id);
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

            // Security check: Ensure the manager is updating an employee in their own branch.
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

            // Update only the allowed fields
            existingEmp.FullName = empFromForm.FullName;
            existingEmp.DateOfBirth = empFromForm.DateOfBirth;
            existingEmp.PhoneNumber = empFromForm.PhoneNumber;
            existingEmp.Email = empFromForm.Email;
            existingEmp.City = empFromForm.City;
            existingEmp.RoleID = newRoleId;

            await _context.SaveChangesAsync();
            return (true, new Dictionary<string, string>());
        }


        // ------------------ PRODUCT ------------------

        public async Task<List<Product>> GetAllProductsWithSizesAsync()
        {
            return await _context.Products
              .Include(p => p.ProductSizes)
              .AsNoTracking() 
              .ToListAsync();
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> CreateProductAsync(Product product)
        {
            var errors = new Dictionary<string, string>();


            if (product.ProductSizes != null && product.ProductSizes.Any())
            {
                var validSizes = new[] { "S", "M", "L" };
                var duplicateSizes = product.ProductSizes.GroupBy(s => s.Size).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                var invalidSizes = product.ProductSizes.Where(s => !validSizes.Contains(s.Size)).Select(s => s.Size).ToList();

                if (duplicateSizes.Any())
                    errors.Add("", $"Các size bị trùng: {string.Join(", ", duplicateSizes)}");
                if (invalidSizes.Any())
                    errors.Add("", $"Các size không hợp lệ: {string.Join(", ", invalidSizes)}");
            }

            if (errors.Any()) return (false, errors);

            product.IsActive = true;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return (true, new Dictionary<string, string>());
        }

        public async Task<(Product? Product, List<ProductCategory> ProductCategories)> GetProductForEditAsync(int id)
        {
            var product = await _context.Products
              .Include(p => p.ProductSizes)
              .FirstOrDefaultAsync(p => p.ProductID == id);

            var categories = await _context.ProductCategories.ToListAsync();

            return (product, categories);
        }

        public async Task<(bool Success, Dictionary<string, string> Errors)> UpdateProductAsync(Product product)
        {
            var errors = new Dictionary<string, string>();

            try
            {
                var existingProduct = await _context.Products
                  .Include(p => p.ProductSizes)
                  .FirstOrDefaultAsync(p => p.ProductID == product.ProductID);

                if (existingProduct == null)
                {
                    errors.Add("", "Product not found.");
                    return (false, errors);
                }


                existingProduct.ProductName = product.ProductName;
                existingProduct.Description = product.Description;
                existingProduct.Image_Url = product.Image_Url;
                existingProduct.CategoryID = product.CategoryID;
                existingProduct.IsActive = product.IsActive;


                _context.ProductSizes.RemoveRange(existingProduct.ProductSizes);


                if (product.ProductSizes != null)
                {
                    existingProduct.ProductSizes = product.ProductSizes.Select(ps => new ProductSize
                    {
                        ProductID = existingProduct.ProductID,
                        Size = ps.Size,
                        Price = ps.Price,
                    }).ToList();
                }
                else
                {
                    existingProduct.ProductSizes = new List<ProductSize>();
                }


                await _context.SaveChangesAsync();
                return (true, new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                errors.Add("", "❌ Error: " + ex.Message);
                return (false, errors);
            }
        }

        // ------------------ CATEGORY ------------------

        public async Task<List<ProductCategory>> GetProductCategoriesAsync()
        {
            return await _context.ProductCategories
                .AsNoTracking() 
                .ToListAsync();
        }


        public async Task HideProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = true;
                await _context.SaveChangesAsync();
            }
        }

        // ------------------ WORK SCHEDULE ------------------

        public async Task<List<WorkSchedule>> GetWorkSchedulesAsync(int branchId, DateTime? startDate, DateTime? endDate)
        {
            {
                var query = _context.WorkSchedules
                    .Include(ws => ws.Employee)
                    .Where(ws => ws.Employee.BranchID == branchId)
                    .AsNoTracking()
                    .AsQueryable();


                if (startDate.HasValue)
                {
                    query = query.Where(ws => ws.WorkDate >= startDate.Value);
                }


                if (endDate.HasValue)
                {
                    query = query.Where(ws => ws.WorkDate <= endDate.Value);
                }


                return await query.OrderBy(ws => ws.WorkDate).ToListAsync();
            }
        }

        public async Task<List<Employee>> GetActiveEmployeesAsync(int branchID){
            return await _context.Employees
              .Where(e => e.IsActive && e.RoleID == "EM" && e.BranchID == branchID)
              .AsNoTracking() // THÊM: Cải thiện hiệu suất
              .ToListAsync();
        }


        public async Task<(bool Success, string? ErrorMessage)> CreateScheduleAsync(WorkSchedule schedule)
        {
            if (schedule.WorkDate < DateTime.Today)
            {
                return (false, "❌ Ngày làm việc không được nhỏ hơn ngày hôm nay.");
            }

            _context.WorkSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return (true, (string?)null);
        }

        public async Task<WorkSchedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.WorkSchedules.FindAsync(id);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateScheduleAsync(WorkSchedule schedule)
        {

            //var originalSchedule = await _context.WorkSchedules.AsNoTracking().FirstOrDefaultAsync(s => s.WorkScheduleID == schedule.WorkScheduleID);


            if (schedule.WorkDate.Date < DateTime.Today.Date)
            {
                return (false, " Ngày làm việc không được chọn là ngày trong quá khứ.");
            }


            _context.WorkSchedules.Update(schedule);
            await _context.SaveChangesAsync();

            return (true, (string?)null);
        }

        public async Task HideScheduleAsync(int id)
        {
            var schedule = await _context.WorkSchedules.FindAsync(id);
            if (schedule != null)
            {
                schedule.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RestoreScheduleAsync(int id)
        {
            var schedule = await _context.WorkSchedules.FindAsync(id);
            if (schedule != null)
            {
                schedule.IsActive = true;
                await _context.SaveChangesAsync();
            }
        }




        //----------------------Create News -----------------
        // public async Task<(bool Success, string? ErrorMessage)> CreateNewsAsync(CreateNews viewModel, string webRootPath)
        // {
        //     try
        //     {
        //         string? uniqueFileName = null;

        //         // 1. Xử lý file upload
        //         if (viewModel.ImageFile != null)
        //         {
        //             // Tạo thư mục nếu chưa tồn tại
        //             string uploadsFolder = Path.Combine(webRootPath, "img/News");
        //             if (!Directory.Exists(uploadsFolder))
        //             {
        //                 Directory.CreateDirectory(uploadsFolder);
        //             }

        //             // Tạo tên file duy nhất
        //             uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ImageFile.FileName;
        //             string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //             // Lưu file
        //             using (var fileStream = new FileStream(filePath, FileMode.Create))
        //             {
        //                 await viewModel.ImageFile.CopyToAsync(fileStream);
        //             }
        //         }

        //         // 2. Tạo đối tượng News từ ViewModel
        //         var news = new News
        //         {
        //             Title = viewModel.Title,
        //             Content = viewModel.Content,
        //             ImageUrl = uniqueFileName != null ? $"/img/News/{uniqueFileName}" : null,
        //             CreatedAt = DateTime.Now
        //         };

        //         // 3. Lưu vào database
        //         _context.News.Add(news);
        //         await _context.SaveChangesAsync();

        //         return (true, null);
        //     }
        //     catch (Exception ex)
        //     {
        //         // Ghi lại log lỗi nếu cần
        //         return (false, "Đã xảy ra lỗi trong quá trình tạo tin tức.");
        //     }
        // }




        // ------------------ SALARY REPORT ------------------

        public async Task<List<SalaryReport>> GetSalaryReportAsync(string? name, int month, int year, double ratePerHour, double hoursPerShift)
        {
            month = month == 0 ? DateTime.Now.Month : month;
            year = year == 0 ? DateTime.Now.Year : year;

            var query = _context.WorkSchedules
              .Include(ws => ws.Employee)
              .Where(ws => ws.IsActive == true &&
                    ws.WorkDate.Month == month &&
                    ws.WorkDate.Year == year)
                    .AsNoTracking();


            if (!string.IsNullOrEmpty(name))
            {

                query = query.Where(ws => ws.Employee != null && ws.Employee.FullName.Contains(name));
            }

            var salaries = await query
              .GroupBy(ws => ws.EmployeeID)
              .Select(g => new SalaryReport
              {
                  EmployeeID = g.Key,
                  FullName = g.First().Employee!.FullName,
                  TotalShifts = g.Count(),
                  TotalHours = g.Count() * hoursPerShift,
                  TotalSalary = g.Count() * hoursPerShift * ratePerHour
              })
              .ToListAsync();

            return salaries;
        }



        // ------------------ REVENUE REPORT ------------------

        public async Task<List<RevenueReport>> GetRevenueReportAsync(int branchId, DateTime startDate, DateTime endDate)
        {
            // Đảm bảo endDate bao gồm cả ngày cuối cùng (đến 23:59:59)
            var inclusiveEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var revenueData = await _context.Orders
                // 1. Lọc theo chi nhánh của người quản lý
                .Where(o => o.BranchID == branchId)

                // 2. Chỉ lấy các đơn hàng đã giao thành công
                .Where(o => o.Status == "Đã Giao")

                // 3. Lọc theo khoảng thời gian người dùng chọn
                .Where(o => o.CreatedAt >= startDate.Date && o.CreatedAt <= inclusiveEndDate)

                // 4. Nhóm các đơn hàng lại theo từng ngày
                .GroupBy(o => o.CreatedAt.Date)

                // 5. Tính toán và tạo ra ViewModel
                .Select(group => new RevenueReport
                {
                    Date = group.Key,
                    TotalOrders = group.Count(),
                    TotalRevenue = group.Sum(o => o.Total)
                })

                // 6. Sắp xếp kết quả theo ngày
                .OrderBy(r => r.Date)
                .ToListAsync();

            return revenueData;
        }


        //Contract
        public async Task<List<Contract>> GetContractsByEmployeeIdAsync(string employeeId, int managerBranchId)
        {

            var employeeExistsInBranch = await _context.Employees
                .AnyAsync(e => e.EmployeeID == employeeId && e.BranchID == managerBranchId);

            if (!employeeExistsInBranch)
            {
                return new List<Contract>();
            }

            // Fetch contracts for the employee
            return await _context.Contracts
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        
        public async Task<(bool success, string message)> CreateContractAsync(Contract contract, int managerBranchId)
{
            // 1. Security Check: Ensure the employee belongs to the manager's branch
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeID == contract.EmployeeId && e.BranchID == managerBranchId);


            if (string.IsNullOrEmpty(contract.Status) || (contract.Status != "Hiệu lực" && contract.Status != "Hết hạn"))
            {
                return (false, "Trạng thái hợp đồng không hợp lệ.");
            }
    
    if (employee == null)
    {
        return (false, "Employee not found in your branch or you don't have permission.");
    }

    // 2. Validation Checks
    if (contract.StartDate > contract.EndDate)
    {
        return (false, "End date cannot be earlier than the start date.");
    }
    if (await _context.Contracts.AnyAsync(c => c.ContractNumber == contract.ContractNumber))
    {
        return (false, "Contract number already exists.");
    }
    // Add other validation as needed (e.g., check ContractType, PaymentType, Status enums/values)

    // 3. Set Server-Side Values
    contract.CreatedAt = DateTime.Now;
    contract.UpdatedAt = null; // Ensure updated_at is null on creation
    // Status might default based on start/end dates, or you can set it here


    // 4. Save to Database
    try
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return (true, "Contract created successfully.");
    }
    catch (DbUpdateException ex)
    {
        // Log the error ex
        return (false, "Database error occurred while saving the contract.");
    }
    catch (Exception ex)
    {
        // Log the error ex
        return (false, "An unexpected error occurred.");
    }
}
        
    }
    
}
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using start.Data;
using start.DTOs;
using start.Models;
using System.Text.Json;
using System.Net;
using start.DTOs.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace start.Services
{
    public class RegionService : IRegionService
    {
        private readonly ApplicationDbContext _db;


        public RegionService(ApplicationDbContext db)
        {
            _db = db;
        }

        private static DateTime GetVietnamTime()
        {
            return DateTime.Now;
        }

        // Thêm điều kiện Status == "Đã giao" vào tất cả query
        private IQueryable<Order> GetFilteredOrders(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null)
        {
            var query = _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Branch)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.Branch.RegionID == regionId
                            && o.CreatedAt >= fromDate
                            && o.CreatedAt <= toDate
                            && o.Status == "Đã giao");

            if (branchId.HasValue)
                query = query.Where(o => o.BranchID == branchId.Value);

            return query;
        }

 


        public async Task<RegionDashboardViewModel?> GetDashboardForManagerAsync(string managerEmployeeId)
        {
            if (string.IsNullOrEmpty(managerEmployeeId)) return null;

            var manager = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == managerEmployeeId);

            if (manager == null) return null;
            if (!manager.RegionID.HasValue) return null;

            int regionId = manager.RegionID.Value;

            var branches = await _db.Branches
                .AsNoTracking()
                .Where(b => b.RegionID == regionId)
                .OrderBy(b => b.Name)
                .ToListAsync();

            // Loại bỏ duplicate theo BranchID (nếu có do lỗi data)
            branches = branches.GroupBy(b => b.BranchID)
                .Select(g => g.First())
                .OrderBy(b => b.Name)
                .ToList();

            var vm = new RegionDashboardViewModel
            {
                ManagerId = managerEmployeeId,
                ManagerName = manager.FullName ?? "Unknown",
                RegionId = regionId,
                RegionName = manager.Region?.RegionName ?? (await _db.Regions.FindAsync(regionId))?.RegionName ?? "Unknown",
                Branches = branches
            };

            return vm;
        }


        public async Task<BranchDetail?> GetBranchDetailsAsync(string managerEmployeeId, int branchId)
        {
            if (string.IsNullOrEmpty(managerEmployeeId)) return null;

            var manager = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == managerEmployeeId);

            if (manager == null || !manager.RegionID.HasValue) return null;

            int regionId = manager.RegionID.Value;

            var branch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BranchID == branchId);

            if (branch == null) return null;
            if (branch.RegionID != regionId) return null; // không cho phép xem

            // tìm branch manager: employee có RoleID == "BM" và BranchID == branchId
            var branchManager = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.RoleID == "BM" && e.BranchID == branchId);

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(e => e.BranchID == branchId && e.RoleID == "EM")
                .OrderBy(e => e.FullName)
                .ToListAsync();

            var dto = new BranchDetail
            {
                BranchID = branch.BranchID,
                Name = branch.Name ?? string.Empty,
                Address = branch.Address,
                City = branch.City,
                Phone = branch.Phone,
                ManagerId = branchManager?.EmployeeID,
                ManagerName = branchManager?.FullName,
                ManagerPhone = branchManager?.PhoneNumber,
                Latitude = branch.Latitude == 0 ? (decimal?)null : branch.Latitude,
                Longitude = branch.Longitude == 0 ? (decimal?)null : branch.Longitude,
                IsActive = branch.IsActive,
                Employees = employees.Select(e => new EmployeeBranchDetail
                {
                    EmployeeID = e.EmployeeID,
                    FullName = e.FullName,
                    DateOfBirth = e.DateOfBirth,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    HireDate = e.HireDate,
                    RoleId = e.RoleID
                }).ToList()
            };

            return dto;
        }




        public async Task<List<BranchStatus>> GetBranchesForStatusAsync(string regionManagerEmployeeId, string? filter = null, string? q = null)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId)) return new List<BranchStatus>();

            var manager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (manager == null || !manager.RegionID.HasValue) return new List<BranchStatus>();

            int regionId = manager.RegionID.Value;

            // Load branches và managers riêng để tránh duplicate
            var branches = await _db.Branches
                .AsNoTracking()
                .Where(b => b.RegionID == regionId)
                .ToListAsync();

            // Load tất cả managers (BM) trong region này
            var branchIds = branches.Select(b => b.BranchID).ToList();
            var managers = await _db.Employees
                .AsNoTracking()
                .Where(e => e.RoleID == "BM" && e.BranchID.HasValue && branchIds.Contains(e.BranchID.Value))
                .ToListAsync();

            // Tạo dictionary để map BranchID -> ManagerName
            var managerDict = managers
                .GroupBy(e => e.BranchID.Value)
                .ToDictionary(g => g.Key, g => g.First().FullName);

            // Tạo BranchStatus list từ branches
            var branchStatusList = branches.Select(b => new BranchStatus
            {
                BranchId = b.BranchID,
                Name = b.Name ?? string.Empty,
                Address = b.Address,
                PhoneNumber = b.Phone,
                IsActive = b.IsActive,
                ManagerName = managerDict.ContainsKey(b.BranchID) ? managerDict[b.BranchID] : null
            }).ToList();

            var query = branchStatusList.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLowerInvariant();
                query = query.Where(x => (x.Name ?? "").ToLower().Contains(s) || ((x.ManagerName ?? "").ToLower().Contains(s)));
            }

            var list = query.ToList();

            // Đảm bảo không có duplicate (mặc dù đã filter trong query)
            list = list.GroupBy(b => b.BranchId)
                .Select(g => g.First())
                .ToList();

            // Apply filter param (controller may already do this, but keep consistent)
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim().ToLowerInvariant();
                if (f == "active") list = list.Where(b => b.IsActive).ToList();
                else if (f == "inactive") list = list.Where(b => !b.IsActive).ToList();
                else if (f == "nomanager") list = list.Where(b => string.IsNullOrEmpty(b.ManagerName) && b.IsActive).ToList();
            }

            return list;
        }


        public async Task<(bool Success, string? Error)> CreateBranchAddRequestAsync(string regionManagerEmployeeId, BranchEditModel model, string? note)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || model == null) return (false, "Dữ liệu không hợp lệ.");

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return (false, "Bạn chưa được gán vùng.");

            var regionId = regionManager.RegionID.Value;

            // Validate Name không được trống
            var branchName = model.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(branchName))
                return (false, "Tên chi nhánh không được để trống.");

            // Validate Latitude và Longitude
            if (model.Latitude.HasValue)
            {
                if (model.Latitude.Value < -90m || model.Latitude.Value > 90m)
                    return (false, "Latitude phải nằm trong khoảng -90 đến 90.");
            }

            if (model.Longitude.HasValue)
            {
                if (model.Longitude.Value < -180m || model.Longitude.Value > 180m)
                    return (false, "Longitude phải nằm trong khoảng -180 đến 180.");
            }

            // Kiểm tra duplicate: Tên chi nhánh không được trùng trong cùng region
            var existingBranch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.RegionID == regionId && 
                                         b.Name != null && 
                                         b.Name.Trim().ToLowerInvariant() == branchName.ToLowerInvariant());

            if (existingBranch != null)
            {
                return (false, $"Đã tồn tại chi nhánh \"{branchName}\" trong khu vực này.");
            }

            // Kiểm tra duplicate: Phone number không được trùng (nếu có phone)
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phone = model.Phone.Trim();
                var existingPhone = await _db.Branches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Phone != null && 
                                             b.Phone.Trim() == phone);

                if (existingPhone != null)
                {
                    return (false, $"Số điện thoại \"{phone}\" đã được sử dụng bởi chi nhánh khác.");
                }
            }

            // Kiểm tra duplicate trong các request đang pending (cùng region, cùng tên)
            var pendingRequest = await _db.BranchRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(br => br.RegionID == regionId && 
                                          br.Status == RequestStatus.Pending &&
                                          br.Name != null &&
                                          br.Name.Trim().ToLowerInvariant() == branchName.ToLowerInvariant());

            if (pendingRequest != null)
            {
                return (false, $"Đã có yêu cầu thêm chi nhánh \"{branchName}\" đang chờ duyệt.");
            }

            var req = new BranchRequest
            {
                RequestType = RequestType.Add,
                BranchId = null,
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                Name = branchName,
                Address = model.Address?.Trim(),
                Phone = model.Phone?.Trim(),
                RegionID = regionId,
                City = model.City?.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                IsActive = true,
                Notes = note?.Trim()
            };

            try
            {
                await _db.AddAsync(req);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> CreateBranchSuspendRequestAsync(string regionManagerEmployeeId, int branchId, string? note)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId)) return (false, "Không hợp lệ.");

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return (false, "Bạn chưa được gán vùng.");

            var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == branchId);
            if (branch == null) return (false, "Chi nhánh không tồn tại.");

            if (regionManager.RegionID.Value != branch.RegionID)
                return (false, "Không có quyền tạm ngừng chi nhánh này.");

            var req = new BranchRequest
            {
                RequestType = RequestType.Edit, // suspend -> Delete request
                BranchId = branchId,
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                Name = branch.Name ?? "",
                Address = branch.Address,
                Phone = branch.Phone,
                RegionID = branch.RegionID,
                City = branch.City,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                IsActive = false, // set IsActive = false for suspend
                Notes = note
            };

            try
            {
                await _db.AddAsync(req);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> CreateBranchPhoneChangeRequestAsync(string regionManagerEmployeeId, int branchId, string newPhone, string? note)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || string.IsNullOrEmpty(newPhone)) return (false, "Dữ liệu không hợp lệ.");

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return (false, "Bạn chưa được gán vùng.");

            var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == branchId);
            if (branch == null) return (false, "Chi nhánh không tồn tại.");
            if (regionManager.RegionID.Value != branch.RegionID) return (false, "Không có quyền.");

            var phone = newPhone.Trim();
            
            // Kiểm tra duplicate phone number (nếu phone mới khác phone cũ)
            if (branch.Phone?.Trim() != phone)
            {
                var existingPhone = await _db.Branches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BranchID != branchId && 
                                             b.Phone != null && 
                                             b.Phone.Trim() == phone);

                if (existingPhone != null)
                {
                    return (false, $"Số điện thoại \"{phone}\" đã được sử dụng bởi chi nhánh khác.");
                }
            }

            var req = new BranchRequest
            {
                RequestType = RequestType.Edit,
                BranchId = branchId,
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                Name = branch.Name ?? "",
                Address = branch.Address,
                Phone = phone, // lưu phone mới proposed
                RegionID = branch.RegionID,
                City = branch.City,
                Latitude = branch.Latitude,
                Longitude = branch.Longitude,
                IsActive = branch.IsActive,
                Notes = note?.Trim()
            };

            try
            {
                await _db.AddAsync(req);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }



        public async Task<List<BranchManagerStatus>> GetBranchManagersForRegionAsync(string managerEmpId, string? q = null)
        {
            if (string.IsNullOrEmpty(managerEmpId)) return new List<BranchManagerStatus>();

            var manager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == managerEmpId);
            if (manager == null || !manager.RegionID.HasValue) return new List<BranchManagerStatus>();

            int regionId = manager.RegionID.Value;

            // 1) Active managers that have a branch in this region
            var activeInRegion = from e in _db.Employees.AsNoTracking()
                                 join b in _db.Branches.AsNoTracking()
                                   on e.BranchID equals b.BranchID
                                 where e.RoleID == "BM" && e.IsActive == true && b.RegionID == regionId
                                 select new BranchManagerStatus
                                 {
                                     EmployeeID = e.EmployeeID,
                                     FullName = e.FullName,
                                     BranchId = e.BranchID,
                                     BranchName = b.Name,
                                     DateOfBirth = e.DateOfBirth,
                                     Gender = e.Gender,
                                     PhoneNumber = e.PhoneNumber,
                                     Email = e.Email,
                                     HireDate = e.HireDate,
                                     IsActive = e.IsActive
                                 };

            // 2) Inactive managers anywhere
            var inactiveAny = from e in _db.Employees.AsNoTracking()
                              join b in _db.Branches.AsNoTracking() on e.BranchID equals b.BranchID into jb
                              from b in jb.DefaultIfEmpty()
                              where e.RoleID == "BM" && e.IsActive == false
                              select new BranchManagerStatus
                              {
                                  EmployeeID = e.EmployeeID,
                                  FullName = e.FullName,
                                  BranchId = e.BranchID,
                                  BranchName = b != null ? b.Name : null,
                                  DateOfBirth = e.DateOfBirth,
                                  Gender = e.Gender,
                                  PhoneNumber = e.PhoneNumber,
                                  Email = e.Email,
                                  HireDate = e.HireDate,
                                  IsActive = e.IsActive
                              };

            // 3) Active managers who currently DON'T have a branch (BranchID == null)
            var activeNoBranch = from e in _db.Employees.AsNoTracking()
                                 where e.RoleID == "BM" && e.IsActive == true && e.BranchID == null
                                 select new BranchManagerStatus
                                 {
                                     EmployeeID = e.EmployeeID,
                                     FullName = e.FullName,
                                     BranchId = (int?)null,
                                     BranchName = null,
                                     DateOfBirth = e.DateOfBirth,
                                     Gender = e.Gender,
                                     PhoneNumber = e.PhoneNumber,
                                     Email = e.Email,
                                     HireDate = e.HireDate,
                                     IsActive = e.IsActive
                                 };

            var unionQuery = activeInRegion.Concat(inactiveAny).Concat(activeNoBranch);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLowerInvariant();
                unionQuery = unionQuery.Where(x =>
                    (x.FullName ?? "").ToLower().Contains(s) ||
                    ((x.BranchName ?? "").ToLower().Contains(s))
                );
            }

            var list = await unionQuery.ToListAsync();

            var result = list.GroupBy(x => x.EmployeeID).Select(g => g.First()).ToList();
            return result;
        }




        // 2) Lấy danh sách candidate (available managers) để đổi quản lý


        public async Task<List<ManagerCandidate>> GetAvailableManagersForRegionAsync(string regionManagerEmployeeId, int branchId, string? q = null)
        {
            var query = from e in _db.Employees.AsNoTracking()
                        join b in _db.Branches.AsNoTracking()
                            on e.BranchID equals b.BranchID into jb
                        from b in jb.DefaultIfEmpty()
                        where e.RoleID == "BM" && (e.IsActive == true && e.BranchID == null)
                        select new ManagerCandidate
                        {
                            EmployeeID = e.EmployeeID,
                            FullName = e.FullName,
                            DateOfBirth = e.DateOfBirth,
                            Gender = null,
                            PhoneNumber = e.PhoneNumber,
                            Email = e.Email,
                            HireDate = e.HireDate,
                            IsActive = e.IsActive,
                            BranchID = e.BranchID,
                            BranchName = b != null ? b.Name : null
                        };

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLowerInvariant();
                query = query.Where(c => (c.FullName ?? "").ToLower().Contains(s) || ((c.BranchName ?? "").ToLower().Contains(s)));
            }

            return await query.ToListAsync();
        }



        // Thay đổi ChangeBranchManagerAsync -> tạo request Edit
        public async Task<bool> ChangeBranchManagerAsync(string regionManagerEmployeeId, int branchId, string newManagerEmployeeId)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || string.IsNullOrEmpty(newManagerEmployeeId))
                return false;

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return false;

            var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == branchId);
            if (branch == null) return false;
            if (branch.RegionID != regionManager.RegionID.Value) return false;

            // check new manager exists and is BM
            var newEmp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == newManagerEmployeeId && e.RoleID == "BM");
            if (newEmp == null) return false;

            var req = new EmployeeBranchRequest
            {
                RequestType = RequestType.Edit,
                EmployeeId = newManagerEmployeeId,
                BranchId = branchId,
                RegionID = branch.RegionID,
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                FullName = newEmp.FullName?.Trim(),
                DateOfBirth = newEmp.DateOfBirth,
                Gender = newEmp.Gender,
                PhoneNumber = newEmp.PhoneNumber,
                Email = newEmp.Email,
                City = newEmp.City,
                Nationality = newEmp.Nationality,
                Ethnicity = newEmp.Ethnicity,
                RoleID = newEmp.RoleID ?? "BM",
            };

            try
            {
                _db.EmployeeBranchRequests.Add(req);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }




        // Cho nghỉ quản lý -> tạo request Edit với BranchId = null, RegionID = null (khách hàng muốn giữ isActive = 1)
        public async Task<bool> DeactivateBranchManagerAsync(string regionManagerEmployeeId, string managerToDeactivateId)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || string.IsNullOrEmpty(managerToDeactivateId))
                return false;

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return false;
            var regionId = regionManager.RegionID.Value;

            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == managerToDeactivateId && e.RoleID == "BM");
            if (emp == null) return false;

            // nếu emp.BranchID != null -> check branch in same region
            if (emp.BranchID != null)
            {
                var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == emp.BranchID.Value);
                if (branch == null) return false;
                if (branch.RegionID != regionId) return false;
            }

            var req = new EmployeeBranchRequest
            {
                RequestType = RequestType.Delete,
                EmployeeId = managerToDeactivateId,
                BranchId = null,      // remove branch assignment
                RegionID = null,      // remove region assignment
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                FullName = emp.FullName?.Trim(),
                DateOfBirth = emp.DateOfBirth,
                Gender = emp.Gender,
                PhoneNumber = emp.PhoneNumber,
                Email = emp.Email,
                City = emp.City,
                Nationality = emp.Nationality,
                Ethnicity = emp.Ethnicity,
                RoleID = emp.RoleID ?? "BM",
            };

            try
            {
                _db.EmployeeBranchRequests.Add(req);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }



        // using start.Models; // ensure namespace
        public async Task<bool> DeleteManagerAsync(string requestedBy, string managerEmployeeId)
        {
            if (string.IsNullOrEmpty(requestedBy) || string.IsNullOrEmpty(managerEmployeeId))
                return false;

            // Kiểm tra quyền: requestedBy phải tồn tại và có RegionID (nếu bạn cần kiểm tra region ownership,
            // bạn có thể validate thêm ở đây). Ở ví dụ này chỉ kiểm tra tồn tại.
            var requester = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == requestedBy);
            if (requester == null)
                return false;

            // Tìm manager target (BM)
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == managerEmployeeId && e.RoleID == "BM");
            if (emp == null) return false;

            // Tạo request Delete (Pending) vào EmployeeBranchRequest
            var req = new EmployeeBranchRequest
            {
                RequestType = RequestType.Edit,
                EmployeeId = managerEmployeeId,
                BranchId = null,
                RegionID = null,
                IsActive = false,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                FullName = emp.FullName?.Trim(),
                DateOfBirth = emp.DateOfBirth,
                Gender = emp.Gender,
                PhoneNumber = emp.PhoneNumber,
                Email = emp.Email,
                City = emp.City,
                Nationality = emp.Nationality,
                Ethnicity = emp.Ethnicity,
                RoleID = emp.RoleID ?? "BM",
            };

            try
            {
                _db.EmployeeBranchRequests.Add(req);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }





        public async Task<int?> CreateBranchAsync(string regionManagerEmployeeId, BranchEditModel model)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || model == null) return null;

            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return null;

            var regionId = regionManager.RegionID.Value;

            var branch = new Branch
            {
                Name = model.Name?.Trim(),
                Address = model.Address?.Trim(),
                Phone = model.Phone?.Trim(),
                City = model.City?.Trim(),
                Latitude = model.Latitude ?? 0m,
                Longitude = model.Longitude ?? 0m,
                RegionID = regionId,
                IsActive = true,
            };

            using (var trx = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    _db.Branches.Add(branch);
                    await _db.SaveChangesAsync();
                    await trx.CommitAsync();
                    return branch.BranchID;
                }
                catch
                {
                    await trx.RollbackAsync();
                    return null;
                }
            }
        }





        public async Task<(bool Success, string? ErrorMessage, string? NewEmployeeId)> CreateManagerAsync(string regionManagerEmployeeId, BranchManagerCreateModel model)
        {
            if (string.IsNullOrEmpty(regionManagerEmployeeId) || model == null)
                return (false, "Dữ liệu không hợp lệ.", null);

            // Kiểm tra quyền region manager
            var regionManager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == regionManagerEmployeeId);
            if (regionManager == null || !regionManager.RegionID.HasValue) return (false, "Bạn chưa được gán khu vực.", null);

            // validate phone/email như trước nhưng không cần sinh EmployeeID
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var phone = model.PhoneNumber.Trim();
                var phoneRegex = new Regex(@"^0\d{9}$");
                if (!phoneRegex.IsMatch(phone))
                    return (false, "Số điện thoại không hợp lệ. Phải bắt đầu bằng '0' và có đúng 10 chữ số.", null);

                var existsPhone = await _db.Employees.AnyAsync(e => e.PhoneNumber == phone);
                if (existsPhone) return (false, "Số điện thoại đã tồn tại.", null);

                model.PhoneNumber = phone;
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var email = model.Email.Trim();
                try { var _ = new System.Net.Mail.MailAddress(email); model.Email = email; }
                catch { return (false, "Email không hợp lệ.", null); }

                var existsEmail = await _db.Employees.AnyAsync(e => e.Email == model.Email);
                if (existsEmail) return (false, "Email đã tồn tại.", null);
            }

            // Tạo EmployeeBranchRequest type = Add
            var req = new EmployeeBranchRequest
            {
                RequestType = RequestType.Add,
                // EmployeeId is null (not created yet)
                BranchId = null,
                RegionID = null, // theo yêu cầu: để null
                FullName = model.FullName?.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                City = model.City,
                Nationality = model.Nationality,
                Ethnicity = model.Ethnicity,
                RoleID = model.RoleID ?? "BM",
                IsActive = model.IsActive,
                RequestedBy = regionManagerEmployeeId,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending
            };

            try
            {
                _db.EmployeeBranchRequests.Add(req);
                await _db.SaveChangesAsync();
                // We do not create Employee yet, so NewEmployeeId = null
                return (true, null, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }


        // ------------------ PRODUCT ------------------

        public async Task<List<Product>> GetAllProductsWithSizesAsync()
        {
            return await _db.Products
              .Include(p => p.ProductSizes)
              .AsNoTracking()
              .ToListAsync();
        }



        public async Task<(Product? Product, List<ProductCategory> ProductCategories)> GetProductForEditAsync(int id)
        { var product = await _db.Products.Include(p => p.ProductSizes).FirstOrDefaultAsync(p => p.ProductID == id); var categories = await _db.ProductCategories.ToListAsync(); return (product, categories); }



        // ------------------ CATEGORY ------------------

        // Lấy danh sách category (dùng để render filter pills)
        public async Task<List<ProductCategory>> GetProductCategoriesAsync()
        {
            return await _db.ProductCategories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }



        public async Task<List<Product>> GetProductsFilteredAsync(int? categoryId, string? q, bool showHidden = false)
        {
            var query = _db.Products
                           .AsNoTracking()
                           .Include(p => p.ProductSizes)
                           .AsQueryable();

            // category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // active / hidden
            if (showHidden)
            {
                // chỉ các sản phẩm isActive == false
                query = query.Where(p => !p.IsActive);
            }
            else
            {
                // mặc định chỉ in active
                query = query.Where(p => p.IsActive);
            }

            // search q
            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLowerInvariant();
                query = query.Where(p => (p.ProductName ?? "").ToLower().Contains(s) || (p.Description ?? "").ToLower().Contains(s));
            }

            return await query.OrderBy(p => p.ProductName).ToListAsync();
        }


        public async Task<(bool Success, string? ErrorMessage)> CreateCategoryRequestAsync(string requestedBy, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(requestedBy) || string.IsNullOrWhiteSpace(categoryName))
                return (false, "Dữ liệu không hợp lệ.");

            var req = new CategoryRequest
            {
                RequestType = RequestType.Add,
                CategoryName = categoryName.Trim(),
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending
            };

            try
            {
                _db.CategoryRequests.Add(req);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateDeleteCategoryRequestAsync(string requestedBy, int categoryId)
        {
            if (string.IsNullOrWhiteSpace(requestedBy))
                return (false, "Dữ liệu không hợp lệ.");

            var cat = await _db.ProductCategories.FindAsync(categoryId);
            if (cat == null) return (false, "Danh mục không tồn tại.");

            var req = new CategoryRequest
            {
                RequestType = RequestType.Delete,
                CategoryId = categoryId,
                CategoryName = cat.CategoryName,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending
            };

            try
            {
                _db.CategoryRequests.Add(req);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }



        // ------------------ REQUEST FOR PRODUCT ------------------

        public async Task<(bool Success, string? Error)> RequestHideProductAsync(int productId, string requestedBy)
        {
            var product = await _db.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null) return (false, "Product not found.");

            var req = new ProductRequest
            {
                RequestType = RequestType.Edit,   // Hide -> mapped to Delete request
                ProductId = product.ProductID,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Image_Url = product.Image_Url,
                IsActive = false, // payload says it's going to be hidden if approved
                ProductSizesJson = product.ProductSizes == null
            ? "[]"
            : JsonSerializer.Serialize(product.ProductSizes.Select(s => new { s.Size, s.Price })
                )
            };

            _db.ProductRequests.Add(req);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RequestRestoreProductAsync(int productId, string requestedBy)
        {
            var product = await _db.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null) return (false, "Product not found.");

            var req = new ProductRequest
            {
                RequestType = RequestType.Edit,   // Restore -> mapped to Edit request
                ProductId = product.ProductID,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Image_Url = product.Image_Url,
                IsActive = true, // payload says restore -> active true when approved
                ProductSizesJson = product.ProductSizes == null
    ? "[]"
    : JsonSerializer.Serialize(product.ProductSizes.Select(s => new { s.Size, s.Price })
                )
            };

            _db.ProductRequests.Add(req);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // Tạo ProductRequest cho "Add" (khi user muốn thêm product)
        public async Task<(bool Success, string? Error)> RequestCreateProductAsync(Product product, string requestedBy)
        {
            if (string.IsNullOrEmpty(requestedBy)) return (false, "RequestedBy required.");

            var req = new ProductRequest
            {
                RequestType = RequestType.Add,
                ProductId = null,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Image_Url = product.Image_Url,
                IsActive = product.IsActive,
                ProductSizesJson = product.ProductSizes == null
    ? "[]"
    : JsonSerializer.Serialize(product.ProductSizes.Select(s => new { s.Size, s.Price }))
            };

            _db.ProductRequests.Add(req);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // Tạo ProductRequest cho "Edit" (khi user sửa product)
        public async Task<(bool Success, string? Error)> RequestEditProductAsync(Product product, string requestedBy)
        {
            if (string.IsNullOrEmpty(requestedBy)) return (false, "RequestedBy required.");

            var existing = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductID == product.ProductID);
            if (existing == null) return (false, "Product không tồn tại.");

            var req = new ProductRequest
            {
                RequestType = RequestType.Edit,
                ProductId = product.ProductID,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                Status = RequestStatus.Pending,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Image_Url = product.Image_Url,
                IsActive = product.IsActive,
                ProductSizesJson = product.ProductSizes == null
    ? "[]"
    : JsonSerializer.Serialize(product.ProductSizes.Select(s => new { s.Size, s.Price }))
            };

            _db.ProductRequests.Add(req);
            await _db.SaveChangesAsync();
            return (true, null);
        }




        // ------------------ REPORTS ------------------


        //Statistics and Reports
        public async Task<IEnumerable<BranchStatisticsDto>> GetBranchStatisticsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null)
        {
            var orders = GetFilteredOrders(regionId, fromDate, toDate, branchId, q);

            if (!string.IsNullOrWhiteSpace(q))
            {
                orders = orders.Where(o => o.OrderDetails.Any(od => od.Product.ProductName.Contains(q)));
            }

            var result = await orders
                .GroupBy(o => new { o.Branch.BranchID, o.Branch.Name })
                .Select(g => new BranchStatisticsDto
                {
                    BranchId = g.Key.BranchID,
                    BranchName = g.Key.Name,
                    OrderCount = g.Select(o => o.OrderID).Distinct().Count(),
                    UnitsSold = g.Sum(o => o.OrderDetails.Sum(od => od.Quantity)),
                    TotalRevenue = g.Sum(o => o.Total) // Sửa: Lấy từ Total của Order thay vì tính UnitPrice * Quantity
                })
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<TopProductDto>> GetTopProductsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null, int top = 10)
        {
            var orders = GetFilteredOrders(regionId, fromDate, toDate, branchId, q);

            if (!string.IsNullOrWhiteSpace(q))
            {
                orders = orders.Where(o => o.OrderDetails.Any(od => od.Product.ProductName.Contains(q)));
            }

            var result = await orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => new { od.Product.ProductID, od.Product.ProductName })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(top)
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<TopCustomerDto>> GetTopCustomersAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null, int top = 10)
        {
            var orders = GetFilteredOrders(regionId, fromDate, toDate, branchId, q);

            if (!string.IsNullOrWhiteSpace(q))
            {
                orders = orders.Where(o =>
                    (o.Customer != null && o.Customer.Name.Contains(q)) ||
                    o.ReceiverPhone.Contains(q));
            }

            var result = await orders
                .GroupBy(o => new { o.CustomerID, CustomerName = o.Customer != null ? o.Customer.Name : "Khách lẻ" })
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key.CustomerID,
                    CustomerName = g.Key.CustomerName,
                    OrderCount = g.Select(o => o.OrderID).Distinct().Count(),
                    TotalSpent = g.Sum(o => o.Total) // Sửa: Lấy từ Total của Order thay vì tính UnitPrice * Quantity
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(top)
                .ToListAsync();

            return result;
        }


        // GetRevenueTrendAsync: group by Y/M/D (EF-friendly)
        public async Task<IEnumerable<RevenueTrendDto>> GetRevenueTrendAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null)
        {
            var orders = GetFilteredOrders(regionId, fromDate, toDate, branchId, q);

            if (!string.IsNullOrWhiteSpace(q))
            {
                orders = orders.Where(o => o.OrderDetails.Any(od => od.Product.ProductName.Contains(q)));
            }

            var grouped = await orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    Revenue = g.Sum(o => o.Total) // Sửa: Lấy từ Total của Order thay vì tính UnitPrice * Quantity
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                .ToListAsync();

            var resDict = grouped.ToDictionary(g => new DateTime(g.Year, g.Month, g.Day), g => g.Revenue);
            var list = new List<RevenueTrendDto>();

            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                var revenue = resDict.TryGetValue(d, out var rev) ? rev : 0m;
                list.Add(new RevenueTrendDto { Date = d, Revenue = revenue });
            }

            return list;
        }

        // GetHourlyHeatmapsAsync: group by Date + Hour and build matrices
        // trong RegionService (hoặc nơi bạn đang implement)
        public async Task<HourlyHeatmapDto> GetHourlyHeatmapsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null)
        {
            var baseQuery = from o in _db.Orders
                            join od in _db.OrderDetails on o.OrderID equals od.OrderID
                            join b in _db.Branches on o.BranchID equals b.BranchID
                            where b.RegionID == regionId
                                  && o.CreatedAt >= fromDate
                                  && o.CreatedAt <= toDate
                            select new { o, od, b };

            if (branchId.HasValue)
                baseQuery = baseQuery.Where(x => x.b.BranchID == branchId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                baseQuery = from x in baseQuery
                            join p in _db.Products on x.od.ProductID equals p.ProductID
                            where p.ProductName.Contains(q)
                            select x;
            }

            // Group by date parts + hour
            // Sửa: Tính Revenue từ Total của Order, Units vẫn tính từ OrderDetails
            var grouped = await baseQuery
                .GroupBy(x => new { x.o.CreatedAt.Year, x.o.CreatedAt.Month, x.o.CreatedAt.Day, Hour = x.o.CreatedAt.Hour, OrderId = x.o.OrderID })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    Hour = g.Key.Hour,
                    OrderId = g.Key.OrderId,
                    Units = g.Sum(y => y.od.Quantity), // Units từ OrderDetails
                    OrderTotal = g.First().o.Total // Revenue lấy từ Total của Order
                })
                .GroupBy(x => new { x.Year, x.Month, x.Day, x.Hour })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Day = g.Key.Day,
                    Hour = g.Key.Hour,
                    Units = g.Sum(x => x.Units), // Tổng Units của tất cả OrderDetails
                    Revenue = g.Sum(x => x.OrderTotal) // Tổng Total của các Order (không tính trùng)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ThenBy(x => x.Hour)
                .ToListAsync();

            // Build a list of distinct dates in order
            var dateList = grouped
                .Select(g => new DateTime(g.Year, g.Month, g.Day))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // If no grouped entries (no data), still build date list from fromDate..toDate
            if (!dateList.Any())
            {
                for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
                    dateList.Add(d);
            }

            var daysStr = dateList.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var matrixUnits = new List<List<int>>();
            var matrixRevenue = new List<List<decimal>>();

            // initialize matrices with zeros
            foreach (var _ in daysStr)
            {
                matrixUnits.Add(Enumerable.Repeat(0, 24).ToList());
                matrixRevenue.Add(Enumerable.Repeat(0m, 24).ToList());
            }

            // index map for date -> row
            var dateIndex = daysStr.Select((d, i) => new { d, i }).ToDictionary(x => x.d, x => x.i);

            foreach (var g in grouped)
            {
                var dayStr = new DateTime(g.Year, g.Month, g.Day).ToString("yyyy-MM-dd");
                if (!dateIndex.TryGetValue(dayStr, out var row)) continue;
                var hour = g.Hour;
                matrixUnits[row][hour] = (int)g.Units;
                matrixRevenue[row][hour] = g.Revenue;
            }

            var dto = new HourlyHeatmapDto
            {
                Days = daysStr,
                MatrixUnits = matrixUnits,
                MatrixRevenue = matrixRevenue
            };

            return dto;
        }


        // Get sent requests but only those created by requestedBy
        public async Task<List<SentRequestListItem>> GetSentRequestsAsync(string requestedBy, RequestCategory? filter = null)
        {
            if (string.IsNullOrWhiteSpace(requestedBy))
                return new List<SentRequestListItem>();

            var list = new List<SentRequestListItem>();

            // --- BranchRequest ---
            if (!filter.HasValue || filter.Value == RequestCategory.Branch)
            {
                var brs = await _db.BranchRequests
                    .AsNoTracking()
                    .Where(x => x.RequestedBy == requestedBy)
                    .OrderByDescending(x => x.RequestedAt)
                    .ToListAsync();

                foreach (var r in brs)
                {
                    // try to fetch branch if BranchId present to read current phone/name
                    string existingBranchName = null;
                    string existingBranchPhone = null;
                    if (r.BranchId.HasValue)
                    {
                        var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchID == r.BranchId.Value);
                        if (branch != null)
                        {
                            existingBranchName = branch.Name;
                            existingBranchPhone = branch.Phone;
                        }
                    }

                    string contentSummaryPlain = "";
                    string contentHtml = "";

                    if ((int)r.RequestType == 2) // Delete -> "Tạm ngừng hoạt động + tên chi nhánh"
                    {
                        var name = r.Name ?? existingBranchName ?? $"Chi nhánh #{r.BranchId}";
                        contentSummaryPlain = $"Tạm ngừng hoạt động {name}";
                        contentHtml = WebUtility.HtmlEncode($"Tạm ngừng hoạt động {name}");
                    }
                    else if ((int)r.RequestType == 1) // Edit -> top: tên, below: đổi SĐT: old -> new
                    {
                        var name = r.Name ?? existingBranchName ?? (r.BranchId.HasValue ? $"Chi nhánh #{r.BranchId}" : "(Không rõ tên)");
                        var oldPhone = existingBranchPhone ?? r.Phone ?? "(không có)";
                        var newPhone = r.Phone ?? "(không có)";
                        contentSummaryPlain = $"{name}\nĐổi SĐT: {oldPhone} -> {newPhone}";
                        contentHtml = WebUtility.HtmlEncode(name) + "<br/>" +
                                      $"Đổi SĐT: {WebUtility.HtmlEncode(oldPhone)} -> {WebUtility.HtmlEncode(newPhone)}";
                    }
                    else // Add (0) -> giữ nguyên: hiển thị tên
                    {
                        var name = r.Name ?? "(Tên chưa có)";
                        contentSummaryPlain = name;
                        contentHtml = WebUtility.HtmlEncode(name);
                    }

                    list.Add(new SentRequestListItem
                    {
                        Id = r.Id,
                        Category = RequestCategory.Branch,
                        RequestType = (int)r.RequestType,
                        RequestTypeLabel = r.RequestType == 0 ? "Thêm" : (int)r.RequestType == 1 ? "Sửa" : "Xóa",
                        ContentSummary = contentSummaryPlain,
                        ContentHtml = contentHtml,
                        RequestedAt = r.RequestedAt,
                        Status = (int)r.Status,
                        StatusLabel = r.Status == 0 ? "Chờ duyệt" : (int)r.Status == 1 ? "Đã duyệt" : "Bị từ chối",
                        RequestedBy = r.RequestedBy
                    });
                }
            }

            // --- EmployeeBranchRequest ---
            if (!filter.HasValue || filter.Value == RequestCategory.EmployeeBranch)
            {
                var ers = await _db.EmployeeBranchRequests
                    .AsNoTracking()
                    .Where(x => x.RequestedBy == requestedBy)
                    .OrderByDescending(x => x.RequestedAt)
                    .ToListAsync();

                foreach (var r in ers)
                {
                    string name = r.FullName ?? r.EmployeeId ?? "(Không rõ)";
                    string contentPlain = "";
                    string contentHtml = "";

                    if ((int)r.RequestType == 0) // Add -> giữ nguyên
                    {
                        contentPlain = $"Thêm quản lý: {name}";
                        contentHtml = WebUtility.HtmlEncode($"Thêm quản lý: {name}");
                    }
                    else if ((int)r.RequestType == 2) // Delete -> "xóa quản lý + tên quản lý"
                    {
                        contentPlain = $"Xóa quản lý {name}";
                        contentHtml = WebUtility.HtmlEncode($"Xóa quản lý {name}");
                    }
                    else // Edit (1): có 2 kiểu
                    {
                        if (!r.BranchId.HasValue)
                        {
                            // deactivation (bỏ chi nhánh) -> find current branch name for that employee (if possible)
                            string currentBranchName = null;
                            if (!string.IsNullOrEmpty(r.EmployeeId))
                            {
                                var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == r.EmployeeId);
                                if (emp != null && emp.BranchID.HasValue)
                                {
                                    var b = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(bb => bb.BranchID == emp.BranchID.Value);
                                    if (b != null) currentBranchName = b.Name;
                                }
                            }
                            currentBranchName ??= "(không rõ chi nhánh)";
                            contentPlain = $"Xóa quản lý {name} khỏi {currentBranchName}";
                            contentHtml = WebUtility.HtmlEncode($"Xóa quản lý {name} khỏi {currentBranchName}");
                        }
                        else
                        {
                            // change assignment -> Thêm quản lý {tên} vào {tên chi nhánh được assign}
                            var targetBranch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(bb => bb.BranchID == r.BranchId.Value);
                            var branchName = targetBranch?.Name ?? $"Chi nhánh #{r.BranchId}";
                            contentPlain = $"Thêm quản lý {name} vào {branchName}";
                            contentHtml = WebUtility.HtmlEncode($"Thêm quản lý {name} vào {branchName}");
                        }
                    }

                    list.Add(new SentRequestListItem
                    {
                        Id = r.Id,
                        Category = RequestCategory.EmployeeBranch,
                        RequestType = (int)r.RequestType,
                        RequestTypeLabel = r.RequestType == 0 ? "Thêm" : (int)r.RequestType == 1 ? "Sửa" : "Xóa",
                        ContentSummary = contentPlain,
                        ContentHtml = contentHtml,
                        RequestedAt = r.RequestedAt,
                        Status = (int)r.Status,
                        StatusLabel = r.Status == 0 ? "Chờ duyệt" : (int)r.Status == 1 ? "Đã duyệt" : "Bị từ chối",
                        RequestedBy = r.RequestedBy
                    });
                }
            }

            // --- ProductRequest ---
            if (!filter.HasValue || filter.Value == RequestCategory.Product)
            {
                if (_db.ProductRequests != null)
                {
                    var prs = await _db.ProductRequests
                        .AsNoTracking()
                        .Where(x => x.RequestedBy == requestedBy)
                        .OrderByDescending(x => x.RequestedAt)
                        .ToListAsync();

                    foreach (var r in prs)
                    {
                        string productName = r.ProductName;
                        if (string.IsNullOrEmpty(productName) && r.ProductId.HasValue)
                        {
                            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(pp => pp.ProductID == r.ProductId.Value);
                            if (p != null) productName = p.ProductName;
                        }
                        productName ??= "(Không rõ)";

                        string contentPlain;
                        string contentHtml = WebUtility.HtmlEncode(productName);

                        if ((int)r.RequestType == 0) contentPlain = $"Thêm sản phẩm: {productName}";
                        else contentPlain = productName; // edit/delete -> show name

                        list.Add(new SentRequestListItem
                        {
                            Id = r.Id,
                            Category = RequestCategory.Product,
                            RequestType = (int)r.RequestType,
                            RequestTypeLabel = r.RequestType == 0 ? "Thêm" : (int)r.RequestType == 1 ? "Sửa" : "Xóa",
                            ContentSummary = contentPlain,
                            ContentHtml = contentHtml,
                            RequestedAt = r.RequestedAt,
                            Status = (int)r.Status,
                            StatusLabel = r.Status == 0 ? "Chờ duyệt" : (int)r.Status == 1 ? "Đã duyệt" : "Bị từ chối",
                            RequestedBy = r.RequestedBy
                        });
                    }
                }
            }

            // --- CategoryRequest ---
            if (!filter.HasValue || filter.Value == RequestCategory.Category)
            {
                var crs = await _db.CategoryRequests
                    .AsNoTracking()
                    .Where(x => x.RequestedBy == requestedBy)
                    .OrderByDescending(x => x.RequestedAt)
                    .ToListAsync();

                foreach (var r in crs)
                {
                    var name = r.CategoryName ?? "(Không rõ)";
                    list.Add(new SentRequestListItem
                    {
                        Id = r.Id,
                        Category = RequestCategory.Category,
                        RequestType = (int)r.RequestType,
                        RequestTypeLabel = r.RequestType == 0 ? "Thêm" : (int)r.RequestType == 1 ? "Sửa" : "Xóa",
                        ContentSummary = name,
                        ContentHtml = WebUtility.HtmlEncode(name),
                        RequestedAt = r.RequestedAt,
                        Status = (int)r.Status,
                        StatusLabel = r.Status == 0 ? "Chờ duyệt" : (int)r.Status == 1 ? "Đã duyệt" : "Bị từ chối",
                        RequestedBy = r.RequestedBy
                    });
                }
            }

            // Sort overall by RequestedAt descending (already mostly sorted per-type, but unify)
            return list.OrderByDescending(x => x.RequestedAt).ToList();
        }

        // Detail: trả thêm canDelete (dành cho view)
        public async Task<SentRequestDetail?> GetSentRequestDetailAsync(string requestedBy, RequestCategory category, int id)
        {
            if (string.IsNullOrWhiteSpace(requestedBy)) return null;

            switch (category)
            {
                case RequestCategory.Branch:
                    {
                        var br = await _db.BranchRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.RequestedBy == requestedBy);
                        if (br == null) return null;

                        // try to get current branch info if BranchId present
                        string currentName = null;
                        string currentPhone = null;
                        if (br.BranchId.HasValue)
                        {
                            var b = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.BranchID == br.BranchId.Value);
                            if (b != null) { currentName = b.Name; currentPhone = b.Phone; }
                        }

                        return new SentRequestDetail
                        {
                            Id = br.Id,
                            Category = RequestCategory.Branch,
                            RequestType = (int)br.RequestType,
                            RequestedBy = br.RequestedBy,
                            RequestedAt = br.RequestedAt,
                            Status = (int)br.Status,
                            ReviewedBy = br.ReviewedBy,
                            ReviewedAt = br.ReviewedAt,
                            RejectionReason = br.RejectionReason,
                            BranchId = br.BranchId,
                            BranchName = br.Name ?? currentName,
                            BranchPhone = br.Phone ?? currentPhone,
                            // allow delete only if requester matches (your controller does this check)
                            CanDelete = true // controller will still enforce permission
                        };
                    }

                case RequestCategory.EmployeeBranch:
                    {
                        var er = await _db.EmployeeBranchRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.RequestedBy == requestedBy);
                        if (er == null) return null;

                        // try to get current branch name if BranchId null but employee exists and assigned
                        string branchName = null;
                        if (!er.BranchId.HasValue && !string.IsNullOrEmpty(er.EmployeeId))
                        {
                            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == er.EmployeeId);
                            if (emp != null && emp.BranchID.HasValue)
                            {
                                var b = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(bb => bb.BranchID == emp.BranchID.Value);
                                if (b != null) branchName = b.Name;
                            }
                        }
                        if (er.BranchId.HasValue)
                        {
                            var tb = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(bb => bb.BranchID == er.BranchId.Value);
                            if (tb != null) branchName = tb.Name;
                        }

                        return new SentRequestDetail
                        {
                            Id = er.Id,
                            Category = RequestCategory.EmployeeBranch,
                            RequestType = (int)er.RequestType,
                            RequestedBy = er.RequestedBy,
                            RequestedAt = er.RequestedAt,
                            Status = (int)er.Status,
                            ReviewedBy = er.ReviewedBy,
                            ReviewedAt = er.ReviewedAt,
                            RejectionReason = er.RejectionReason,
                            EmployeeId = er.EmployeeId,
                            FullName = er.FullName,
                            PhoneNumber = er.PhoneNumber,
                            BranchId = er.BranchId,
                            RegionId = er.RegionID,
                            BranchName = branchName,
                            CanDelete = true
                        };
                    }

                case RequestCategory.Product:
                    if (_db.ProductRequests == null) return null;

                    var pr = await _db.ProductRequests
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == id && x.RequestedBy == requestedBy);

                    if (pr == null) return null;

                    var detail = new SentRequestDetail
                    {
                        Id = pr.Id,
                        Category = RequestCategory.Product,
                        RequestType = (int)pr.RequestType,
                        RequestedBy = pr.RequestedBy,
                        RequestedAt = pr.RequestedAt,
                        Status = (int)pr.Status,
                        ReviewedBy = pr.ReviewedBy,
                        ReviewedAt = pr.ReviewedAt,
                        RejectionReason = pr.RejectionReason,
                        ProductId = pr.ProductId,
                        ProductName = pr.ProductName,
                        ProductDescription = pr.Description,      // nếu tên trường khác hãy đổi
                        ProductImageUrl = pr.Image_Url,            // hoặc pr.Image_Url
                        ProductCategoryId = pr.CategoryID,
                        ProductCategoryName = pr.ProductName     // có thể null nếu bạn chỉ lưu Id
                    };

                    // sizes: nhiều hệ thống lưu sizes như JSON trong ProductRequest.SizesJson
                    try
                    {
                        // Option A: nếu có field SizesJson (JSON array of { size, price })
                        if (!string.IsNullOrWhiteSpace(pr.ProductSizesJson))
                        {
                            var parsed = JsonSerializer.Deserialize<List<ProductSizeDto>>(pr.ProductSizesJson);
                            detail.ProductSizes = parsed ?? new List<ProductSizeDto>();
                        }
                        else
                        {
                            detail.ProductSizes = new List<ProductSizeDto>();
                        }

                    }
                    catch
                    {
                        detail.ProductSizes = new List<ProductSizeDto>();
                    }

                    // optional: decide canDelete for client
                    detail.CanDelete = true;

                    return detail;

                case RequestCategory.Category:
                    {
                        var cr = await _db.CategoryRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.RequestedBy == requestedBy);
                        if (cr == null) return null;
                        return new SentRequestDetail
                        {
                            Id = cr.Id,
                            Category = RequestCategory.Category,
                            RequestType = (int)cr.RequestType,
                            RequestedBy = cr.RequestedBy,
                            RequestedAt = cr.RequestedAt,
                            Status = (int)cr.Status,
                            ReviewedBy = cr.ReviewedBy,
                            ReviewedAt = cr.ReviewedAt,
                            RejectionReason = cr.RejectionReason,
                            CategoryId = cr.CategoryId,
                            CategoryName = cr.CategoryName,
                            CanDelete = true
                        };
                    }

                default:
                    return null;
            }
        }

        // Delete request only if requestedBy matches (or you may allow admin — here only requester allowed)
        public async Task<(bool Success, string? ErrorMessage)> DeleteSentRequestAsync(string requestedBy, RequestCategory category, int id)
        {
            if (string.IsNullOrWhiteSpace(requestedBy))
                return (false, "Unauthorized");

            try
            {
                switch (category)
                {
                    case RequestCategory.Branch:
                        {
                            var br = await _db.BranchRequests.FirstOrDefaultAsync(x => x.Id == id);
                            if (br == null) return (false, "Không tìm thấy request.");
                            if (!string.Equals(br.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
                                return (false, "Bạn không có quyền xóa request này.");
                            _db.BranchRequests.Remove(br);
                            break;
                        }

                    case RequestCategory.EmployeeBranch:
                        {
                            var er = await _db.EmployeeBranchRequests.FirstOrDefaultAsync(x => x.Id == id);
                            if (er == null) return (false, "Không tìm thấy request.");
                            if (!string.Equals(er.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
                                return (false, "Bạn không có quyền xóa request này.");
                            _db.EmployeeBranchRequests.Remove(er);
                            break;
                        }

                    case RequestCategory.Product:
                        {
                            if (_db.ProductRequests == null) return (false, "Không hỗ trợ Product requests.");
                            var pr = await _db.ProductRequests.FirstOrDefaultAsync(x => x.Id == id);
                            if (pr == null) return (false, "Không tìm thấy request.");
                            if (!string.Equals(pr.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
                                return (false, "Bạn không có quyền xóa request này.");
                            _db.ProductRequests.Remove(pr);
                            break;
                        }

                    case RequestCategory.Category:
                        {
                            var cr = await _db.CategoryRequests.FirstOrDefaultAsync(x => x.Id == id);
                            if (cr == null) return (false, "Không tìm thấy request.");
                            if (!string.Equals(cr.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
                                return (false, "Bạn không có quyền xóa request này.");
                            _db.CategoryRequests.Remove(cr);
                            break;
                        }

                    default:
                        return (false, "Loại request không hợp lệ.");
                }

                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                // log ex nếu cần
                return (false, ex.Message);
            }
        }


        public async Task<List<Branch>> GetBranchesForRegionAsync(int regionId)
        {
            return await _db.Branches
                .AsNoTracking()
                .Where(b => b.RegionID == regionId)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }






    }
}



using System.Threading.Tasks;
using start.DTOs;
using start.Models;

namespace start.Services
{
  public interface IRegionService
  {
    Task<RegionDashboardViewModel?> GetDashboardForManagerAsync(string managerEmployeeId);

    Task<BranchDetail?> GetBranchDetailsAsync(string managerEmployeeId, int branchId);

    Task<List<BranchStatus>> GetBranchesForStatusAsync(string managerEmpId, string filter = "all", string? q = null);




    // lấy danh sách quản lý chi nhánh trong region 
    Task<List<BranchManagerStatus>> GetBranchManagersForRegionAsync(string managerEmpId, string? q = null);


    // Lấy danh sách ứng viên quản lý (inactive) trong cùng region của region manager.

    Task<List<ManagerCandidate>> GetAvailableManagersForRegionAsync(string regionManagerEmployeeId, int branchId, string? q = null);


    /// Thay đổi quản lý cho branch: remove current manager (đặt IsActive = false),

    Task<bool> ChangeBranchManagerAsync(string regionManagerEmployeeId, int branchId, string newManagerEmployeeId);

    // Cho nghỉ 1 manager: only if the manager belongs to the region of regionManagerEmployeeId
    Task<bool> DeactivateBranchManagerAsync(string regionManagerEmployeeId, string managerToDeactivateId);

    // Xóa quản lý (chuyển trạng thái về 'Chưa có chi nhánh')
    Task<bool> DeleteManagerAsync(string requestedBy, string managerEmployeeId);

    //Tạo quản lý chi nhánh mới
    Task<(bool Success, string? ErrorMessage, string? NewEmployeeId)> CreateManagerAsync(string regionManagerEmployeeId, BranchManagerCreateModel model);

    // Product
    Task<List<Product>> GetAllProductsWithSizesAsync();
    Task<List<ProductCategory>> GetProductCategoriesAsync();

    Task<(Product? Product, List<ProductCategory> ProductCategories)> GetProductForEditAsync(int id);



    Task<List<Product>> GetProductsFilteredAsync(int? categoryId, string? q, bool showHidden);

    //request Product
    Task<(bool Success, string? Error)> RequestHideProductAsync(int productId, string requestedBy);
    Task<(bool Success, string? Error)> RequestRestoreProductAsync(int productId, string requestedBy);

    Task<(bool Success, string? Error)> RequestCreateProductAsync(Product product, string requestedBy);

    Task<(bool Success, string? Error)> RequestEditProductAsync(Product product, string requestedBy);

    //request category
    Task<(bool Success, string? ErrorMessage)> CreateCategoryRequestAsync(string requestedBy, string categoryName);
    Task<(bool Success, string? ErrorMessage)> CreateDeleteCategoryRequestAsync(string requestedBy, int categoryId);

    //request branch
    Task<(bool Success, string? Error)> CreateBranchAddRequestAsync(string regionManagerEmployeeId, BranchEditModel model, string? note);
    Task<(bool Success, string? Error)> CreateBranchSuspendRequestAsync(string regionManagerEmployeeId, int branchId, string? note);
    Task<(bool Success, string? Error)> CreateBranchPhoneChangeRequestAsync(string regionManagerEmployeeId, int branchId, string newPhone, string? note);


    //statistics
    Task<IEnumerable<BranchStatisticsDto>> GetBranchStatisticsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null);
    Task<IEnumerable<TopProductDto>> GetTopProductsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null, int top = 10);
    Task<IEnumerable<TopCustomerDto>> GetTopCustomersAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null, int top = 10);
    Task<IEnumerable<RevenueTrendDto>> GetRevenueTrendAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null);
    Task<HourlyHeatmapDto> GetHourlyHeatmapsAsync(int regionId, DateTime fromDate, DateTime toDate, int? branchId = null, string? q = null);


    //request detail
    // thêm hoặc cập nhật
    Task<List<SentRequestListItem>> GetSentRequestsAsync(string requestedBy, RequestCategory? filter = null);
    Task<SentRequestDetail?> GetSentRequestDetailAsync(string requestedBy, RequestCategory category, int id);
    Task<(bool Success, string? ErrorMessage)> DeleteSentRequestAsync(string requestedBy, RequestCategory category, int id);


 
  }
}

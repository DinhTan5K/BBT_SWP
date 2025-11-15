    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using start.Services;
    using start.Models;
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using start.DTOs;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc.ViewEngines;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    using System.IO;
    using System.Text.Encodings.Web;
    using PuppeteerSharp;
    using PuppeteerSharp.Media;
    using DocumentFormat.OpenXml.ExtendedProperties;
    using start.Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Authorization;

    namespace start.Controllers
    {
        [Authorize(AuthenticationSchemes = "RegionManagerScheme")]
        public class RegionController : Controller
        {
            private readonly IRegionService _regionService;

            private readonly IAntiforgery _antiforgery;

            private readonly IConfiguration _configuration;
            private readonly ICompositeViewEngine _viewEngine;
            private string? CurrentEmpId => HttpContext.Session.GetString("EmployeeID");
            private int? CurrentRegionId => HttpContext.Session.GetInt32("RegionID");
            public RegionController(IRegionService regionService, IConfiguration configuration, IAntiforgery antiforgery, ICompositeViewEngine viewEngine)
            {
                _regionService = regionService;

                _configuration = configuration;

                _antiforgery = antiforgery;
                _viewEngine = viewEngine;

            }


            /// Trang chính của Region Manager:

            [HttpGet]
            public async Task<IActionResult> RegionHome(string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId))
                    return RedirectToAction("Login", "Account");

                // Lấy VM từ service (bao gồm danh sách tất cả branch thuộc region)
                RegionDashboardViewModel? vm = await _regionService.GetDashboardForManagerAsync(empId);
                if (vm == null)
                    return View("NoRegionAssigned");

                // Preserve current filter/search for initial render (view có thể highlight các nút)
                ViewData["CurrentFilter"] = (filter ?? "all").Trim().ToLowerInvariant();
                ViewData["CurrentQuery"] = q ?? "";

                return View(vm);
            }

            // Partial để cập nhật danh sách chi nhánh theo filter + search (AJAX)
            [HttpGet]
            public async Task<IActionResult> RegionGridPartial(string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId))
                    return Unauthorized(); // client nên redirect nếu cần

                var vm = await _regionService.GetDashboardForManagerAsync(empId);
                if (vm == null)
                {
                    // Nếu manager chưa có vùng -> trả partial rỗng để client hiển thị không có chi nhánh
                    return PartialView("_BranchGridPartial", new List<Branch>());
                }

                var f = (filter ?? "all").Trim().ToLowerInvariant();
                var search = (q ?? "").Trim();

                var branches = vm.Branches ?? new List<Branch>();

                // Loại bỏ duplicate theo BranchID trước khi filter/search
                branches = branches.GroupBy(b => b.BranchID)
                    .Select(g => g.First())
                    .ToList();

                // Apply filter
                if (f == "active")
                    branches = branches.Where(b => b.IsActive).ToList();
                else if (f == "inactive")
                    branches = branches.Where(b => !b.IsActive).ToList();

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    var s = search.ToLowerInvariant();
                    branches = branches.Where(b => (b.Name ?? "").ToLowerInvariant().Contains(s)).ToList();
                }

                // Trả partial view với model là IEnumerable<Branch>
                return PartialView("_BranchGridPartial", branches);
            }


            // Hiển thị khi Region Manager chưa được gán vùng
            [HttpGet]
            public IActionResult NoRegionAssigned()
            {
                return View();
            }


            // Chi tiết chi nhánh - gọi service trả BranchDetail DTO cho empId (để đảm bảo manager chỉ xem branch trong vùng của họ)        
            [HttpGet]
            public async Task<IActionResult> BranchDetail(int id)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                var dto = await _regionService.GetBranchDetailsAsync(empId, id);
                if (dto == null)
                {
                    // Nếu null: branch không tồn tại hoặc không thuộc region của manager -> 403
                    return Forbid();
                }

                return View("BranchDetail", dto);
            }


            // ==================== MANAGE BRANCH STATUS ====================

            // GET /Region/ManageBranchStatus

            // ManageBranchStatus GET
            [HttpGet]
            public async Task<IActionResult> ManageBranchStatus(string? view, string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId))
                    return RedirectToAction("Login", "Account");

                // normalize: default branch-filter -> "active", default manager-filter -> "active"
                var f = (filter ?? "active").Trim().ToLowerInvariant();
                var query = (q ?? "").Trim();

                // Always fetch branches (we will pass to view). Service returns all branches in region.
                var branches = await _regionService.GetBranchesForStatusAsync(empId, null, query);

                // Loại bỏ duplicate theo BranchId (đảm bảo an toàn)
                branches = branches.GroupBy(b => b.BranchId)
                    .Select(g => g.First())
                    .ToList();

                // Determine requested tab
                var activeTab = string.IsNullOrWhiteSpace(view) ? "branches" : view.Trim().ToLowerInvariant();
                ViewData["ActiveTab"] = activeTab;
                ViewData["CurrentQuery"] = query; // global search

                if (activeTab == "branches")
                {
                    // Apply branch server-side filtering so the server-rendered branch grid matches selected filter
                    if (f == "active")
                        branches = branches.Where(b => b.IsActive).ToList();
                    else if (f == "inactive")
                        branches = branches.Where(b => !b.IsActive).ToList();
                    else if (f == "nomanager" || f == "nomanager") // allow alias
                        branches = branches.Where(b => string.IsNullOrEmpty(b.ManagerName)).ToList();

                    ViewData["CurrentFilter"] = f;
                    return View("ManageBranchStatus", branches);
                }
                else
                {
                    // activeTab == "managers": fetch managers initial and provide to view in ViewBag
                    var mgrList = await _regionService.GetBranchManagersForRegionAsync(empId, query);

                    // Apply manager filter: active / inactive / nobranch
                    if (f == "active")
                        mgrList = mgrList.Where(x => x.IsActive).ToList();
                    else if (f == "inactive")
                        mgrList = mgrList.Where(x => !x.IsActive).ToList();
                    else if (f == "nobranch")
                        mgrList = mgrList.Where(x => x.IsActive && x.BranchId == null).ToList();

                    ViewBag.ManagersInitial = mgrList;
                    ViewData["CurrentFilter"] = f;
                    return View("ManageBranchStatus", branches); // branches still passed for the model type; managers are in ViewBag
                }
            }




            // Partial để cập nhật danh sách chi nhánh theo filter + search (AJAX)

            public async Task<IActionResult> BranchStatusPartial(string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId))
                    return Unauthorized();

                var f = (filter ?? "active").Trim().ToLowerInvariant();
                var query = (q ?? "").Trim();

                var branches = await _regionService.GetBranchesForStatusAsync(empId, null, query);

                // Loại bỏ duplicate theo BranchId (đảm bảo an toàn)
                branches = branches.GroupBy(b => b.BranchId)
                    .Select(g => g.First())
                    .ToList();

                if (f == "active")
                    branches = branches.Where(b => b.IsActive).ToList();
                else if (f == "inactive")
                    branches = branches.Where(b => !b.IsActive).ToList();
                else if (f == "nomanager")
                    // CHỈ chọn chi nhánh đang hoạt động và chưa có quản lý
                    branches = branches.Where(b => b.IsActive && string.IsNullOrEmpty(b.ManagerName)).ToList();

                ViewData["CurrentFilter"] = f;
                ViewData["CurrentQuery"] = query;

                return PartialView("_BranchStatusPartial", branches);
            }




            // POST: create suspend request (Ajax or form post)
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateSuspendBranchRequest(int branchId, string? note)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId))
                    return Json(new { success = false, message = "Không xác thực được người dùng." });

                var (success, error) = await _regionService.CreateBranchSuspendRequestAsync(empId, branchId, note);

                if (success)
                {
                    return Json(new { success = true, message = "Yêu cầu tạm ngừng hoạt động chi nhánh đã được gửi." });
                }

                return Json(new { success = false, message = "Không tạo được yêu cầu: " + (error ?? "Lỗi không xác định.") });
            }



            // GET /Region/ManageBranchManagers
            [HttpGet]
            public async Task<IActionResult> ManageBranchManagers(string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                var f = (filter ?? "active").Trim().ToLowerInvariant();
                var query = (q ?? "").Trim();

                // Lấy danh sách managers (service trả một tập hợp ban đầu)
                var list = await _regionService.GetBranchManagersForRegionAsync(empId, query);

                // Áp filter server-side: active / inactive / nobranch
                if (f == "active")
                {
                    list = list.Where(x => x.IsActive).ToList();
                }
                else if (f == "inactive")
                {
                    list = list.Where(x => !x.IsActive).ToList();
                }
                else if (f == "nobranch")
                {
                    // Yêu cầu của bạn: show managers who are active (isActive == 1) but BranchID == null
                    list = list.Where(x => x.IsActive && (x.BranchId == null)).ToList();
                }

                ViewData["CurrentFilter"] = f;
                ViewData["CurrentQuery"] = query;

                return View("ManageBranchManagers", list);
            }


            // Partial: trả HTML chỉ phần grid managers (dùng cho AJAX)
            [HttpGet]
            public async Task<IActionResult> BranchManagersPartial(string? filter, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                var f = (filter ?? "active").Trim().ToLowerInvariant();
                var query = (q ?? "").Trim();

                // Lấy list theo service (service trả 3 nhóm: active in region, inactive anywhere, active no-branch)
                var list = await _regionService.GetBranchManagersForRegionAsync(empId, query);

                // Áp filter cụ thể:
                if (f == "active")
                {
                    // Chỉ managers "đang làm": BranchId != null AND IsActive == true
                    list = list.Where(x => x.IsActive && x.BranchId != null).ToList();
                }
                else if (f == "nobranch")
                {
                    // CHÚ Ý: theo yêu cầu mới -> Chỉ managers chưa có chi nhánh nhưng vẫn active:
                    // BranchId == null AND IsActive == true
                    list = list.Where(x => x.IsActive && x.BranchId == null).ToList();
                }

                ViewData["CurrentFilter"] = f;
                ViewData["CurrentQuery"] = query;

                return PartialView("_BranchManagersPartial", list);
            }



            [HttpGet]
            public async Task<IActionResult> ChangeBranchManager(int id) // id = branchId
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                // kiểm tra quyền: RegionService có method GetDashboardForManagerAsync
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return View("NoRegionAssigned");

                var branch = dashboard.Branches?.FirstOrDefault(b => b.BranchID == id);
                if (branch == null) return Forbid();


                var candidates = await _regionService.GetAvailableManagersForRegionAsync(empId, id);


                string? currentMgrId = null;
                string? currentMgrName = null;

                var branchDetail = await _regionService.GetBranchDetailsAsync(empId, id);
                if (branchDetail != null)
                {

                    try
                    {
                        var prop = branchDetail.GetType().GetProperty("ManagerName");
                        if (prop != null) currentMgrName = prop.GetValue(branchDetail)?.ToString();
                    }
                    catch { /* ignore */ }
                }

                var vm = new ChangeBranchManager
                {
                    BranchId = id,
                    BranchName = branch.Name ?? $"Branch {id}",
                    CurrentManagerId = currentMgrId,
                    CurrentManagerName = currentMgrName,
                    Candidates = candidates
                };

                return View("ChangeBranchManager", vm);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ChangeBranchManager(int branchId, string newManagerId)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                if (string.IsNullOrEmpty(newManagerId))
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn một quản lý để thay thế.";
                    return RedirectToAction("ChangeBranchManager", new { id = branchId });
                }

                // gọi service để tạo request (service sẽ validate quyền & dữ liệu)
                var ok = await _regionService.ChangeBranchManagerAsync(empId, branchId, newManagerId);
                if (!ok)
                {
                    TempData["ErrorMessage"] = "Không thể tạo yêu cầu thay đổi quản lý. Vui lòng thử lại.";
                    return RedirectToAction("ChangeBranchManager", new { id = branchId });
                }

                TempData["SuccessMessage"] = "Đã gửi yêu cầu thay đổi quản lý tới admin.";
                return RedirectToAction("ManageBranchStatus");
            }



            //cho nghỉ quản lý chi nhánh (POST)


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeactivateManager([FromForm] string managerId)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                if (string.IsNullOrEmpty(managerId))
                {
                    TempData["ErrorMessage"] = "Manager id is required.";
                    return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "inactive" });
                }

                var ok = await _regionService.DeactivateBranchManagerAsync(empId, managerId);
                if (!ok)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(new { success = false, error = "Không thể tạo yêu cầu cho nghỉ quản lý (kiểm tra quyền hoặc dữ liệu)." });
                    TempData["ErrorMessage"] = "Không thể tạo yêu cầu cho nghỉ quản lý (kiểm tra quyền hoặc dữ liệu).";
                    return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "inactive" });
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đã gửi yêu cầu cho nghỉ quản lý." });

                TempData["SuccessMessage"] = "Đã gửi yêu cầu cho nghỉ quản lý.";
                return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "nobranch" });
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteManager([FromForm] string managerId)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                if (string.IsNullOrEmpty(managerId))
                {
                    TempData["ErrorMessage"] = "Manager id is required.";
                    return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "inactive" });
                }

                // Gọi service để tạo request (requestedBy = CurrentEmpId)
                var ok = await _regionService.DeleteManagerAsync(empId, managerId);
                if (!ok)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(new { success = false, error = "Không thể tạo yêu cầu xóa quản lý." });

                    TempData["ErrorMessage"] = "Không thể tạo yêu cầu xóa quản lý.";
                    return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "inactive" });
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đã gửi yêu cầu xóa quản lý." });

                TempData["SuccessMessage"] = "Đã gửi yêu cầu xóa quản lý.";
                return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "inactive" });
            }




            // Tạo chi nhánh mới(Dùng lại EditBranch View)
            [HttpGet]
            public async Task<IActionResult> AddBranch()
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return View("NoRegionAssigned");
                var model = new BranchEditModel { BranchID = 0 };
                return View("EditBranch", model);
            }


            // POST: AddBranch -> tạo branch request (Add)
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddBranch(BranchEditModel model, string? note)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");
                if (!ModelState.IsValid) return View("EditBranch", model);

                var (success, error) = await _regionService.CreateBranchAddRequestAsync(empId, model, note);
                if (success)
                {
                    TempData["SuccessMessage"] = "Yêu cầu thêm chi nhánh đã được gửi tới admin để duyệt.";
                    return RedirectToAction("ManageBranchStatus", new { view = "branches" });
                }

                TempData["ErrorMessage"] = "Không tạo được yêu cầu: " + (error ?? "");
                return View("EditBranch", model);
            }

            // POST: create phone change request
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateChangeBranchPhoneRequest(int branchId, string newPhone, string? note)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                var (success, error) = await _regionService.CreateBranchPhoneChangeRequestAsync(empId, branchId, newPhone, note);
                if (success) return Json(new { success = true, message = "Yêu cầu đổi số điện thoại đã được gửi." });
                return Json(new { success = false, message = error ?? "Không thể tạo yêu cầu." });
            }






            // GET
            [HttpGet]
            public async Task<IActionResult> AddBranchManager()
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return View("NoRegionAssigned");

                var vm = new BranchManagerCreateModel
                {
                    HireDate = DateTime.Now,
                    RoleID = "BM",
                    IsActive = true
                };

                // Render view AddManager.cshtml (consistent name)
                return View("AddBranchManager", vm);
            }

            // POST
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddBranchManager(BranchManagerCreateModel model)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                if (!ModelState.IsValid)
                {
                    // return the same view so validation messages show
                    return View("AddBranchManager", model);
                }

                var (success, errorMessage, newId) = await _regionService.CreateManagerAsync(empId, model);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Đã gửi yêu cầu thêm quản lý mới";
                    return RedirectToAction("ManageBranchStatus", new { view = "managers", filter = "active" });
                }

                // Show error returned from service
                ModelState.AddModelError(string.Empty, errorMessage ?? "Không thể tạo quản lý (lỗi không xác định).");
                return View("AddBranchManager", model);
            }



            // ------------------ PRODUCT CRUD ------------------


            [HttpGet]
            public async Task<IActionResult> ViewProduct(int? categoryId, string? q, bool? showHidden)
            {
                var products = await _regionService.GetProductsFilteredAsync(categoryId, q, showHidden ?? false);

                ViewBag.Categories = await _regionService.GetProductCategoriesAsync();

                ViewData["CurrentCategory"] = categoryId?.ToString() ?? "all";
                ViewData["CurrentQuery"] = q ?? "";
                ViewData["ShowHidden"] = (showHidden ?? false) ? "1" : "0";

                return View(products);
            }

            // Trong RegionController
            [HttpGet]
            [HttpGet]
            public async Task<IActionResult> FilterProductsPartial(int? categoryId, string? q, bool? showHidden)
            {
                var products = await _regionService.GetProductsFilteredAsync(categoryId, q, showHidden ?? false);
                return PartialView("_ProductTablePartial", products);
            }



            [HttpGet]
            // SỬA: Chuyển sang async Task
            public async Task<IActionResult> CreateProduct()
            {
                // SỬA: Gọi phiên bản async
                ViewBag.Categories = await _regionService.GetProductCategoriesAsync();
                return View();
            }


            [HttpGet]
            public async Task<IActionResult> CreateProductPartial()
            {
                ViewBag.Categories = new SelectList(await _regionService.GetProductCategoriesAsync(), "CategoryID", "CategoryName");

                // truyền cloud info cho partial
                ViewBag.Cloudinary_CloudName = _configuration["CloudinarySettings:CloudName"] ?? "";
                ViewBag.Cloudinary_UploadPreset = _configuration["CloudinarySettings:UploadPreset"] ?? "";

                return PartialView("_CreateProductPartial", new Product());
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateProduct(Product product)
            {
                product.ProductSizes = product.ProductSizes?
                    .Where(ps => !string.IsNullOrEmpty(ps.Size) && ps.Price > 0)
                    .ToList() ?? new List<ProductSize>();

                if (ModelState.IsValid)
                {
                    var empId = CurrentEmpId;
                    if (string.IsNullOrEmpty(empId)) return Unauthorized();

                    var (success, error) = await _regionService.RequestCreateProductAsync(product, empId);
                    if (success) return Json(new { success = true, message = "Đã tạo yêu cầu thêm sản phẩm, chờ duyệt." });

                    ModelState.AddModelError(string.Empty, error ?? "Không thể tạo request.");
                }

                ViewBag.Categories = new SelectList(await _regionService.GetProductCategoriesAsync(), "CategoryID", "CategoryName");
                return PartialView("_CreateProductPartial", product);
            }


            [HttpGet]
            public async Task<IActionResult> EditProduct(int id)
            {
                var (product, categories) = await _regionService.GetProductForEditAsync(id);
                if (product == null) return NotFound();

                ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);

                // truyền cloud info
                ViewBag.Cloudinary_CloudName = _configuration["CloudinarySettings:CloudName"] ?? "";
                ViewBag.Cloudinary_UploadPreset = _configuration["CloudinarySettings:UploadPreset"] ?? "";

                return View(product);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> EditProduct(Product product)
            {
                product.ProductSizes = product.ProductSizes?
                    .Where(ps => !string.IsNullOrEmpty(ps.Size) && ps.Price > 0)
                    .ToList() ?? new List<ProductSize>();

                if (ModelState.IsValid)
                {
                    var empId = CurrentEmpId;
                    if (string.IsNullOrEmpty(empId)) return Unauthorized();

                    var (success, error) = await _regionService.RequestEditProductAsync(product, empId);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Đã tạo yêu cầu sửa sản phẩm, chờ duyệt.";
                        return RedirectToAction("ViewProduct");
                    }

                    ModelState.AddModelError(string.Empty, error ?? "Không thể tạo request sửa.");
                }

                var (p, categories) = await _regionService.GetProductForEditAsync(product.ProductID);
                ViewBag.Categories = new SelectList(categories, "CategoryID", "CategoryName", product.CategoryID);

                return View(product);
            }


            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> HideProduct(int id)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                var (success, error) = await _regionService.RequestHideProductAsync(id, empId);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(new { success = false, error });
                    TempData["ErrorMessage"] = error ?? "Không thể tạo request hide.";
                    return RedirectToAction("ViewProduct");
                }

                // return JSON success for AJAX
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đã tạo yêu cầu ẩn sản phẩm, chờ duyệt." });

                TempData["SuccessMessage"] = "Đã tạo yêu cầu ẩn sản phẩm, chờ duyệt.";
                return RedirectToAction("ViewProduct");
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> RestoreProduct(int id)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                var (success, error) = await _regionService.RequestRestoreProductAsync(id, empId);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return BadRequest(new { success = false, error });
                    TempData["ErrorMessage"] = error ?? "Không thể tạo request restore.";
                    return RedirectToAction("ViewProduct");
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đã tạo yêu cầu phục hồi sản phẩm, chờ duyệt." });

                TempData["SuccessMessage"] = "Đã tạo yêu cầu phục hồi sản phẩm, chờ duyệt.";
                return RedirectToAction("ViewProduct");
            }


            // ==================== Category ====================
            [HttpGet]
            public async Task<IActionResult> GetCategoriesJson()
            {
                try
                {
                    var cats = await _regionService.GetProductCategoriesAsync();
                    var dto = cats.Select(c => new { CategoryID = c.CategoryID, CategoryName = c.CategoryName }).ToList();
                    return Json(dto);
                }
                catch (Exception ex)
                {
                    // trả lỗi để client hiển thị (để debug). Production: log và trả thông điệp chung.
                    return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> CreateCategoryRequest([FromForm] string categoryName)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Json(new { success = false, message = "Unauthorized" });

                if (string.IsNullOrWhiteSpace(categoryName))
                    return Json(new { success = false, message = "Tên danh mục không được để trống." });

                try
                {
                    var (ok, err) = await _regionService.CreateCategoryRequestAsync(empId, categoryName.Trim());
                    if (ok) return Json(new { success = true, message = "Yêu cầu tạo danh mục đã được gửi." });
                    return Json(new { success = false, message = err ?? "Không thể tạo yêu cầu." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteCategoryRequest([FromForm] int categoryId)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Json(new { success = false, message = "Unauthorized" });

                if (categoryId <= 0) return Json(new { success = false, message = "CategoryId không hợp lệ." });

                try
                {
                    var (ok, err) = await _regionService.CreateDeleteCategoryRequestAsync(empId, categoryId);
                    if (ok) return Json(new { success = true, message = "Yêu cầu xóa danh mục đã được gửi." });
                    return Json(new { success = false, message = err ?? "Không thể tạo yêu cầu xóa." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Server error: " + ex.Message });
                }
            }






            // GET: /Region/Statistics
            [HttpGet]
            public async Task<IActionResult> Statistics(DateTime? from, DateTime? to)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return RedirectToAction("Login", "Account");

                // Lấy RegionID từ Employee thay vì từ session
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return View("NoRegionAssigned");

                var regionId = dashboard.RegionId;

                var f = from ?? DateTime.Now.AddDays(-7);
                var t = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

                // gọi service để lấy dữ liệu ban đầu hiển thị (7 ngày default)
                var branchStats = await _regionService.GetBranchStatisticsAsync(regionId, f, t);
                var topProducts = await _regionService.GetTopProductsAsync(regionId, f, t);
                var revenueTrend = await _regionService.GetRevenueTrendAsync(regionId, f, t);
                var heatmap = await _regionService.GetHourlyHeatmapsAsync(regionId, f, t);
                var topCustomers = await _regionService.GetTopCustomersAsync(regionId, f, t);


                var model = new RegionStatisticsViewModel
                {
                    From = f,
                    To = t,
                    BranchStats = branchStats.ToList(),
                    TopProducts = topProducts.ToList(),
                    RevenueTrend = revenueTrend.ToList(),
                    HourlyHeatmap = heatmap,
                    TopCustomers = topCustomers.ToList()
                };
                model.Branches = await _regionService.GetBranchesForRegionAsync(regionId);

                return View(model);
            }

            [HttpGet]
            public async Task<IActionResult> GetStatisticsData(DateTime? from, DateTime? to, int? branchId, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return BadRequest("Not authenticated");

                // Lấy RegionID từ Employee thay vì từ session
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return BadRequest("No region assigned");

                var regionId = dashboard.RegionId;

                var f = from ?? DateTime.Now.AddDays(-7);
                var t = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

                var branchStats = await _regionService.GetBranchStatisticsAsync(regionId, f, t, branchId, q);
                var topProducts = await _regionService.GetTopProductsAsync(regionId, f, t, branchId, q);
                var revenueTrend = await _regionService.GetRevenueTrendAsync(regionId, f, t, branchId, q);
                var topCustomers = await _regionService.GetTopCustomersAsync(regionId, f, t, branchId, q);

                var totals = new
                {
                    totalRevenue = branchStats.Sum(b => b.TotalRevenue),
                    totalOrders = branchStats.Sum(b => b.OrderCount),
                    totalUnits = branchStats.Sum(b => b.UnitsSold),
                    aov = branchStats.Sum(b => b.OrderCount) == 0 ? 0m : branchStats.Sum(b => b.TotalRevenue) / branchStats.Sum(b => b.OrderCount)
                };

                return Json(new
                {
                    branchStats,
                    topProducts,
                    revenueTrend,
                    topCustomers,
                    totals
                });
            }

            [HttpGet]
            public async Task<IActionResult> ExportStatisticsCsv(DateTime? from, DateTime? to, int? branchId, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return BadRequest("Not authenticated");

                // Lấy RegionID từ Employee thay vì từ session
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return BadRequest("No region assigned");

                var regionId = dashboard.RegionId;

                var f = from ?? DateTime.Now.AddDays(-7);
                var t = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

                // Lấy dữ liệu
                var branchStats = await _regionService.GetBranchStatisticsAsync(regionId, f, t, branchId, q);
                var topProducts = await _regionService.GetTopProductsAsync(regionId, f, t, branchId, q, top: 1000);
                var revenueTrend = await _regionService.GetRevenueTrendAsync(regionId, f, t, branchId, q);
                var topCustomers = await _regionService.GetTopCustomersAsync(regionId, f, t, branchId, q, top: 1000);

                // Tính tổng
                var totalRevenue = branchStats.Sum(b => b.TotalRevenue);
                var totalOrders = branchStats.Sum(b => b.OrderCount);
                var totalUnits = branchStats.Sum(b => b.UnitsSold);
                var aov = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

                // Tạo CSV
                var sb = new StringBuilder();

                // BOM để Excel nhận UTF-8
                var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());

                sb.AppendLine("BÁO CÁO DOANH THU & THỐNG KÊ");
                sb.AppendLine($"Từ ngày:,{f:dd/MM/yyyy}");
                sb.AppendLine($"Đến ngày:,{t:dd/MM/yyyy}");
                sb.AppendLine($"Chi nhánh:,{(branchId.HasValue ? branchStats.FirstOrDefault(b => b.BranchId == branchId)?.BranchName ?? "Tất cả" : "Tất cả")}");
                sb.AppendLine();

                // KPI
                sb.AppendLine("TỔNG QUAN");
                sb.AppendLine($"Doanh thu,{totalRevenue:N0} ₫");
                sb.AppendLine($"Số đơn hàng,{totalOrders}");
                sb.AppendLine($"Số sản phẩm bán,{totalUnits}");
                sb.AppendLine($"AOV,{aov:N0} ₫");
                sb.AppendLine();

                // Doanh thu theo chi nhánh
                sb.AppendLine("DOANH THU THEO CHI NHÁNH");
                sb.AppendLine("Chi nhánh,Đơn hàng,Sản phẩm bán,Doanh thu");
                foreach (var b in branchStats)
                {
                    sb.AppendLine($"{EscapeCsv(b.BranchName)},{b.OrderCount},{b.UnitsSold},{b.TotalRevenue:N0}");
                }
                sb.AppendLine();

                // Top sản phẩm
                sb.AppendLine("TOP SẢN PHẨM");
                sb.AppendLine("Sản phẩm,Số lượng,Doanh thu");
                foreach (var p in topProducts)
                {
                    sb.AppendLine($"{EscapeCsv(p.ProductName)},{p.TotalSold},{p.TotalRevenue:N0}");
                }
                sb.AppendLine();

                // Top khách hàng
                sb.AppendLine("KHÁCH HÀNG ĐẶT NHIỀU NHẤT");
                sb.AppendLine("Khách hàng,Số đơn,Tổng chi tiêu");
                foreach (var c in topCustomers)
                {
                    sb.AppendLine($"{EscapeCsv(c.CustomerName)},{c.OrderCount},{c.TotalSpent:N0}");
                }

                // Chuyển thành byte[]
                var content = sb.ToString();
                var bytes = Encoding.UTF8.GetBytes(content);

                // Gộp BOM + nội dung
                var finalBytes = bom.Concat(bytes).ToArray();

                var fileName = $"bao-cao-doanh-thu_{f:yyyyMMdd}_{t:yyyyMMdd}.csv";

                return File(finalBytes, "text/csv; charset=utf-8", fileName);
            }

            // HÀM ESCAPE CSV AN TOÀN 100%
            private string EscapeCsv(string? input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return "";

                // Nếu chứa dấu phẩy, ngoặc kép, xuống dòng → bọc trong ""
                if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
                {
                    // Thay " thành ""
                    var escaped = input.Replace("\"", "\"\"");
                    return $"\"{escaped}\"";
                }

                return input;
            }

            // PDF export: render view that can be printed to PDF or used by server-side pdf library
            // GET /Region/ExportStatisticsPdf
            [HttpGet]
            public async Task<IActionResult> ExportStatisticsPdf(DateTime? from, DateTime? to, int? branchId, string? q)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return BadRequest("Not authenticated");

                // Lấy RegionID từ Employee thay vì từ session
                var dashboard = await _regionService.GetDashboardForManagerAsync(empId);
                if (dashboard == null) return BadRequest("No region assigned");

                var regionId = dashboard.RegionId;

                var f = from ?? DateTime.Now.AddDays(-7);
                var t = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

                var branchStats = await _regionService.GetBranchStatisticsAsync(regionId, f, t, branchId, q);
                var topProducts = await _regionService.GetTopProductsAsync(regionId, f, t, branchId, q, 100);
                var revenueTrend = await _regionService.GetRevenueTrendAsync(regionId, f, t, branchId, q);
                var topCustomers = await _regionService.GetTopCustomersAsync(regionId, f, t, branchId, q, 100);

                var model = new RegionStatisticsViewModel
                {
                    From = f,
                    To = t,
                    BranchStats = branchStats.ToList(),
                    TopProducts = topProducts.ToList(),
                    RevenueTrend = revenueTrend.ToList(),
                    TopCustomers = topCustomers.ToList()
                };

                string branchName = branchId.HasValue
                    ? branchStats.FirstOrDefault(b => b.BranchId == branchId)?.BranchName ?? "Không xác định"
                    : "Tất cả chi nhánh";
                ViewBag.BranchName = branchName;

                // Render HTML
                var html = await RenderViewToStringAsync("ExportStatisticsPdf", model);

                // Tạo PDF
                var pdfBytes = await GeneratePdfFromHtml(html);

                var fileName = $"bao-cao-doanh-thu_{f:yyyyMMdd}_{t:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }


            // GET: /Region/SentRequests
            [HttpGet]
            public IActionResult SentRequests()
            {
                // view sẽ load dữ liệu bằng AJAX
                return View("SentRequests");
            }

            // API: load list (json) optionally filter by category
            [HttpGet]
            public async Task<IActionResult> GetSentRequests([FromQuery] RequestCategory? category)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Json(new List<object>());

                try
                {
                    var list = await _regionService.GetSentRequestsAsync(empId, category);
                    return Json(list);
                }
                catch (Exception ex)
                {
                    // log ex nếu cần
                    return StatusCode(500, new { message = "Lỗi server khi lấy danh sách." });
                }
            }

            [HttpGet]
            public async Task<IActionResult> GetSentRequestDetail([FromQuery] RequestCategory category, [FromQuery] int id)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                try
                {
                    var detail = await _regionService.GetSentRequestDetailAsync(empId, category, id);
                    if (detail == null) return NotFound(new { message = "Không tìm thấy request hoặc bạn không có quyền xem." });
                    return Json(detail);
                }
                catch (Exception ex)
                {
                    // log ex
                    return StatusCode(500, new { message = "Lỗi server khi lấy chi tiết request." });
                }
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteSentRequest([FromForm] int category, [FromForm] int id)
            {
                var empId = CurrentEmpId;
                if (string.IsNullOrEmpty(empId)) return Json(new { success = false, message = "Unauthorized" });

                try
                {
                    if (!Enum.IsDefined(typeof(RequestCategory), category))
                        return Json(new { success = false, message = "Loại request không hợp lệ." });

                    var cat = (RequestCategory)category;
                    var (ok, err) = await _regionService.DeleteSentRequestAsync(empId, cat, id);

                    if (ok) return Json(new { success = true, message = "Đã xóa yêu cầu." });
                    return Json(new { success = false, message = err ?? "Không thể xóa yêu cầu." });
                }
                catch (Exception ex)
                {
                    // log ex
                    return StatusCode(500, new { success = false, message = "Lỗi server khi xóa request." });
                }
            }

            // 1. Render View → HTML String
            // 1. Render View → HTML String
            private async Task<string> RenderViewToStringAsync(string viewName, object model)
            {
                var httpContext = ControllerContext.HttpContext;
                var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), ControllerContext.ActionDescriptor);

                using var writer = new StringWriter();
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.View == null)
                    throw new FileNotFoundException($"View '{viewName}' not found.");

                var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                // Copy ViewData
                foreach (var kvp in ViewData)
                    viewData[kvp.Key] = kvp.Value;

                // Copy TempData (ĐÃ SỬA)
                var tempData = TempData;

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    tempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.ToString();
            }

            // 2. HTML → PDF
            private async Task<byte[]> GeneratePdfFromHtml(string html)
            {
                // Tự động tải Chromium lần đầu
                await new BrowserFetcher().DownloadAsync();

                await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--font-render-hinting=medium" }
                });

                await using var page = await browser.NewPageAsync();

                // Đặt nội dung HTML
                await page.SetContentAsync(html);

                // ĐỢI BIỂU ĐỒ RENDER XONG (Chart.js cần thời gian)
                await Task.Delay(1500); // THAY THẾ WaitForTimeoutAsync

                // Tạo PDF
                return await page.PdfDataAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    MarginOptions = new MarginOptions
                    {
                        Top = "1.5cm",
                        Bottom = "1.5cm",
                        Left = "1.5cm",
                        Right = "1.5cm"
                    }
                });
            }

            

        }


    }

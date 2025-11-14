# Tổng hợp thay đổi: Validation Duplicate & Currency Formatting

## Ngày: [Hôm nay]

## 1. VALIDATION DUPLICATE - Branch Name & Phone

### 1.1. RegionService.cs - CreateBranchAddRequestAsync
**File**: `Services/RegionManager/RegionService.cs`
**Vị trí**: Dòng 220-315

**Thay đổi**:
- ✅ Thêm validation: Tên chi nhánh không được trống
- ✅ Thêm validation duplicate: Tên chi nhánh không được trùng trong cùng region (case-insensitive)
- ✅ Thêm validation duplicate: Phone number không được trùng trong toàn bộ hệ thống
- ✅ Thêm validation: Kiểm tra duplicate trong các request đang pending (cùng region, cùng tên)

**Code mẫu**:
```csharp
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
```

### 1.2. RegionService.cs - CreateBranchPhoneChangeRequestAsync
**File**: `Services/RegionManager/RegionService.cs`
**Vị trí**: Dòng 360-416

**Thay đổi**:
- ✅ Thêm validation duplicate phone: Chỉ kiểm tra nếu phone mới khác phone cũ
- ✅ Trim phone number trước khi validate và lưu

**Code mẫu**:
```csharp
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
```

### 1.3. AdminController.cs - ApproveBranchRequest
**File**: `Controllers/AdminController.cs`
**Vị trí**: Dòng 2764-2868

**Thay đổi**:
- ✅ Cải thiện validation duplicate name: Case-insensitive comparison
- ✅ Thêm validation duplicate phone khi Add branch
- ✅ Thêm validation duplicate phone khi Edit branch
- ✅ Trim tất cả các fields trước khi lưu

**Code mẫu (Add)**:
```csharp
// Kiểm tra tên branch không trùng trong cùng region (case-insensitive)
var existingBranch = await _db.Branches
    .FirstOrDefaultAsync(b => b.Name != null && 
                             b.Name.Trim().ToLowerInvariant() == request.Name.Trim().ToLowerInvariant() && 
                             b.RegionID == request.RegionID);

if (existingBranch != null)
{
    return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{request.Name}\" trong khu vực này" });
}

// Kiểm tra duplicate phone number (nếu có phone)
if (!string.IsNullOrWhiteSpace(request.Phone))
{
    var phone = request.Phone.Trim();
    var existingPhone = await _db.Branches
        .FirstOrDefaultAsync(b => b.Phone != null && b.Phone.Trim() == phone);

    if (existingPhone != null)
    {
        return Json(new { success = false, message = $"Số điện thoại \"{phone}\" đã được sử dụng bởi chi nhánh khác" });
    }
}
```

**Code mẫu (Edit)**:
```csharp
// Kiểm tra tên branch không trùng với branch khác trong cùng region (case-insensitive)
var branchName = request.Name?.Trim() ?? "";
var existingBranch = await _db.Branches
    .FirstOrDefaultAsync(b => b.Name != null &&
                             b.Name.Trim().ToLowerInvariant() == branchName.ToLowerInvariant() &&
                             b.RegionID == request.RegionID &&
                             b.BranchID != request.BranchId.Value);

if (existingBranch != null)
{
    return Json(new { success = false, message = $"Đã tồn tại chi nhánh \"{branchName}\" trong khu vực này" });
}

// Kiểm tra duplicate phone number (nếu phone mới khác phone cũ)
if (!string.IsNullOrWhiteSpace(request.Phone))
{
    var phone = request.Phone.Trim();
    if (branch.Phone?.Trim() != phone)
    {
        var existingPhone = await _db.Branches
            .FirstOrDefaultAsync(b => b.BranchID != request.BranchId.Value &&
                                     b.Phone != null &&
                                     b.Phone.Trim() == phone);

        if (existingPhone != null)
        {
            return Json(new { success = false, message = $"Số điện thoại \"{phone}\" đã được sử dụng bởi chi nhánh khác" });
        }
    }
}
```

### 1.4. AdminController.cs - AddBranch & EditBranch
**File**: `Controllers/AdminController.cs`
**Vị trí**: Dòng 1270-1357

**Thay đổi**:
- ✅ Validation duplicate name đã có sẵn (case-sensitive)
- ⚠️ **NOTE**: Nên cải thiện thành case-insensitive để nhất quán

**Hiện tại**:
```csharp
var existingBranch = await _db.Branches
    .FirstOrDefaultAsync(b => b.Name.Trim() == Name.Trim() && b.RegionID == RegionID);
```

**Nên cải thiện thành**:
```csharp
var existingBranch = await _db.Branches
    .FirstOrDefaultAsync(b => b.Name != null && 
                             b.Name.Trim().ToLowerInvariant() == Name.Trim().ToLowerInvariant() && 
                             b.RegionID == RegionID);
```

---

## 2. DATABASE CONSTRAINTS - Unique Indexes

### 2.1. ApplicationDbContext.cs - OnModelCreating
**File**: `Data/ApplicationDbContext.cs`
**Vị trí**: Dòng 59-80

**Thay đổi**:
- ✅ Thêm method `OnModelCreating` để cấu hình database constraints
- ✅ Thêm index cho Branch (RegionID, Name) để tăng performance
- ✅ Thêm unique index cho Branch Phone (với filter NULL)

**Code**:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Unique constraint: Branch Name trong cùng Region (case-insensitive)
    // Note: SQL Server không hỗ trợ case-insensitive unique constraint trực tiếp,
    // nên chúng ta sẽ validate trong code thay vì database constraint
    // Tuy nhiên, có thể thêm index để tăng performance
    modelBuilder.Entity<Branch>()
        .HasIndex(b => new { b.RegionID, b.Name })
        .HasDatabaseName("IX_Branch_RegionID_Name");

    // Unique constraint: Branch Phone (nếu có)
    // Chỉ áp dụng cho phone không null
    modelBuilder.Entity<Branch>()
        .HasIndex(b => b.Phone)
        .HasDatabaseName("IX_Branch_Phone")
        .IsUnique()
        .HasFilter("[Phone] IS NOT NULL");

    // Employee đã có unique constraints trong model (PhoneNumber, Email)
}
```

**Lưu ý**:
- Index `IX_Branch_RegionID_Name` giúp tăng performance khi query duplicate
- Unique index `IX_Branch_Phone` đảm bảo phone không trùng ở database level
- Filter `[Phone] IS NOT NULL` cho phép nhiều NULL values (theo SQL standard)

---

## 3. CURRENCY FORMATTING - Helper Class

### 3.1. CurrencyHelper.cs - New File
**File**: `Helpers/CurrencyHelper.cs`
**Vị trí**: New file

**Thay đổi**:
- ✅ Tạo helper class mới để format currency
- ✅ 3 methods: `FormatVND`, `FormatVNDNoUnit`, `FormatVNDShort`

**Code**:
```csharp
using System.Globalization;

namespace start.Helpers
{
    public static class CurrencyHelper
    {
        /// <summary>
        /// Format số tiền thành chuỗi VNĐ (ví dụ: 100000 -> "100,000 VNĐ")
        /// </summary>
        public static string FormatVND(decimal? amount)
        {
            if (amount == null) return "0 VNĐ";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture) + " VNĐ";
        }

        /// <summary>
        /// Format số tiền thành chuỗi VNĐ không có đơn vị (ví dụ: 100000 -> "100,000")
        /// </summary>
        public static string FormatVNDNoUnit(decimal? amount)
        {
            if (amount == null) return "0";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format số tiền thành chuỗi với ký hiệu đ (ví dụ: 100000 -> "100,000 đ")
        /// </summary>
        public static string FormatVNDShort(decimal? amount)
        {
            if (amount == null) return "0 đ";
            return amount.Value.ToString("N0", CultureInfo.InvariantCulture) + " đ";
        }
    }
}
```

### 3.2. _ViewImports.cshtml - Add Using
**File**: `Views/_ViewImports.cshtml`
**Vị trí**: Dòng 3

**Thay đổi**:
- ✅ Thêm `@using start.Helpers` để sử dụng CurrencyHelper trong tất cả views

**Code**:
```cshtml
@using start
@using start.Models
@using start.Helpers
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

### 3.3. Sử dụng CurrencyHelper trong Views

**Cách sử dụng**:
```cshtml
<!-- Thay vì -->
@totalRevenue.ToString("N0") ₫

<!-- Dùng -->
@CurrencyHelper.FormatVND(totalRevenue)
<!-- hoặc -->
@CurrencyHelper.FormatVNDShort(totalRevenue)
```

**Ví dụ trong Statistics.cshtml**:
```cshtml
<!-- Trước -->
<h4 id="kpiRevenue">@totalRevenue.ToString("N0") ₫</h4>

<!-- Sau -->
<h4 id="kpiRevenue">@CurrencyHelper.FormatVNDShort(totalRevenue)</h4>
```

---

## 4. TỔNG HỢP CÁC LỖI ĐÃ FIX

### 4.1. Validation Issues
- ✅ **Fixed**: Branch name có thể trùng (case-sensitive)
- ✅ **Fixed**: Branch phone có thể trùng
- ✅ **Fixed**: Không validate duplicate trong pending requests
- ✅ **Fixed**: Không validate duplicate phone khi change phone

### 4.2. Database Issues
- ✅ **Added**: Unique index cho Branch Phone
- ✅ **Added**: Index cho Branch (RegionID, Name) để tăng performance
- ✅ **Note**: Case-insensitive validation phải làm trong code (SQL Server không hỗ trợ)

### 4.3. Currency Formatting Issues
- ✅ **Created**: CurrencyHelper class để format currency nhất quán
- ✅ **Added**: Using statement trong _ViewImports.cshtml
- ⚠️ **TODO**: Cập nhật tất cả views để sử dụng CurrencyHelper (đang trong progress)

---

## 5. CẦN LÀM TIẾP

### 5.1. AdminController.cs - AddBranch & EditBranch
- ⚠️ Cải thiện validation duplicate name thành case-insensitive
- ⚠️ Thêm validation duplicate phone (nếu chưa có)

### 5.2. Currency Formatting
- ⚠️ Cập nhật tất cả views để sử dụng CurrencyHelper
  - Views/Admin/*.cshtml
  - Views/BManager/*.cshtml
  - Views/Region/*.cshtml
  - Views/Employee/*.cshtml
  - Views/Product/*.cshtml
  - Views/Order/*.cshtml

### 5.3. Database Migration
- ⚠️ Tạo migration để apply unique index cho Branch Phone
- ⚠️ Tạo migration để apply index cho Branch (RegionID, Name)

---

## 6. TESTING CHECKLIST

### 6.1. Validation Testing
- [ ] Test: Tạo branch với tên trùng (case-insensitive) -> Should fail
- [ ] Test: Tạo branch với phone trùng -> Should fail
- [ ] Test: Tạo request với tên trùng đang pending -> Should fail
- [ ] Test: Edit branch với phone trùng -> Should fail
- [ ] Test: Edit branch với tên trùng (case-insensitive) -> Should fail

### 6.2. Currency Formatting Testing
- [ ] Test: FormatVND với null -> Should return "0 VNĐ"
- [ ] Test: FormatVND với số lớn -> Should format correctly (1,000,000 VNĐ)
- [ ] Test: FormatVNDShort với số lớn -> Should format correctly (1,000,000 đ)
- [ ] Test: FormatVNDNoUnit với số lớn -> Should format correctly (1,000,000)

### 6.3. Database Testing
- [ ] Test: Insert branch với phone trùng -> Should fail (unique constraint)
- [ ] Test: Query performance với index -> Should be faster

---

## 7. FILES CHANGED

1. ✅ `Services/RegionManager/RegionService.cs`
   - `CreateBranchAddRequestAsync`: Thêm validation duplicate
   - `CreateBranchPhoneChangeRequestAsync`: Thêm validation duplicate phone

2. ✅ `Controllers/AdminController.cs`
   - `ApproveBranchRequest`: Cải thiện validation duplicate (case-insensitive, phone)

3. ✅ `Data/ApplicationDbContext.cs`
   - `OnModelCreating`: Thêm unique index cho Branch Phone, index cho (RegionID, Name)

4. ✅ `Helpers/CurrencyHelper.cs`
   - New file: Helper class để format currency

5. ✅ `Views/_ViewImports.cshtml`
   - Thêm `@using start.Helpers`

---

## 8. NOTES

- **Case-insensitive validation**: SQL Server không hỗ trợ case-insensitive unique constraint trực tiếp, nên phải validate trong code
- **Phone unique constraint**: Chỉ áp dụng cho phone không null (filter `[Phone] IS NOT NULL`)
- **Currency formatting**: Nên sử dụng CurrencyHelper để nhất quán format trong toàn bộ project
- **Database migration**: Cần tạo migration để apply các index mới

---

## 9. NEXT STEPS

1. ✅ Hoàn thành validation duplicate
2. ⚠️ Cập nhật tất cả views để sử dụng CurrencyHelper
3. ⚠️ Tạo database migration
4. ⚠️ Test toàn bộ validation và currency formatting
5. ⚠️ Review và fix các lỗi còn lại


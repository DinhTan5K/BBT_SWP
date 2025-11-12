# Giải thích chi tiết: Tính năng Bật/Tắt sản phẩm (Toggle IsActive)

## 📋 Tổng quan
Tính năng này cho phép admin bật/tắt trạng thái hiển thị của sản phẩm trên website mà không cần xóa sản phẩm khỏi database.

---

## 1️⃣ **Model - Product.cs**
**File:** `Models/Products/Product.cs`

```csharp
public bool IsActive { get; set; } = true;
```

- **Kiểu dữ liệu:** `bool` (true/false)
- **Giá trị mặc định:** `true` (sản phẩm mới tạo sẽ tự động được kích hoạt)
- **Mục đích:** Xác định sản phẩm có đang được bán hay không

---

## 2️⃣ **View - Products.cshtml**
**File:** `Views/Admin/Products.cshtml`

### Phần hiển thị trạng thái (dòng 64-75):
```razor
@if (product.IsActive)
{
    <span style="...">Đang bán</span>
}
else
{
    <span style="...">Ngừng bán</span>
}
```
- Hiển thị badge màu xanh "Đang bán" nếu `IsActive = true`
- Hiển thị badge màu đỏ "Ngừng bán" nếu `IsActive = false`

### Nút Bật/Tắt (dòng 79-82):
```razor
<button onclick="toggleProductStatus(@product.ProductID, @product.IsActive.ToString().ToLower())" 
        style="background: @(product.IsActive ? "#f59e0b" : "#10b981");">
    @(product.IsActive ? "Tắt" : "Bật")
</button>
```

**Giải thích:**
- `onclick="toggleProductStatus(...)"` - Gọi hàm JavaScript khi click
- `@product.ProductID` - Truyền ID sản phẩm để server biết sản phẩm nào cần cập nhật
- `@product.IsActive.ToString().ToLower()` - Truyền trạng thái hiện tại (true/false)
- **Màu nút:**
  - Màu cam (#f59e0b) + chữ "Tắt" khi `IsActive = true`
  - Màu xanh lá (#10b981) + chữ "Bật" khi `IsActive = false`

---

## 3️⃣ **JavaScript Function - toggleProductStatus()**
**File:** `Views/Admin/Products.cshtml` (dòng 129-155)

```javascript
function toggleProductStatus(productId, currentStatus) {
    // 1. Xác nhận với người dùng
    if (!confirm('Bạn có chắc muốn thay đổi trạng thái sản phẩm này?')) {
        return; // Nếu người dùng hủy, dừng lại
    }

    // 2. Lấy Anti-Forgery Token để bảo mật
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // 3. Gửi request POST đến server
    fetch('@Url.Action("ToggleProductStatus", "Admin")', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `productId=${productId}&__RequestVerificationToken=${encodeURIComponent(token)}`
    })
    .then(response => response.json()) // Chuyển response thành JSON
    .then(data => {
        if (data.success) {
            alert(data.message); // Hiển thị thông báo thành công
            location.reload(); // Reload trang để cập nhật UI
        } else {
            alert(data.message || 'Có lỗi xảy ra');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Có lỗi xảy ra khi cập nhật trạng thái');
    });
}
```

**Luồng hoạt động:**
1. User click nút → Hàm `toggleProductStatus()` được gọi
2. Hiển thị dialog xác nhận
3. Lấy Anti-Forgery Token từ form
4. Gửi POST request đến `/Admin/ToggleProductStatus` với `productId`
5. Nhận JSON response từ server
6. Hiển thị thông báo và reload trang nếu thành công

---

## 4️⃣ **Controller Action - ToggleProductStatus()**
**File:** `Controllers/AdminController.cs` (dòng 436-456)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleProductStatus(int productId)
{
    // 1. Kiểm tra quyền đăng nhập
    if (string.IsNullOrEmpty(CurrentEmpId))
        return Json(new { success = false, message = "Bạn cần đăng nhập" });

    // 2. Tìm sản phẩm trong database
    var product = await _db.Products.FindAsync(productId);
    if (product == null)
        return Json(new { success = false, message = "Sản phẩm không tồn tại" });

    // 3. Đảo ngược trạng thái IsActive
    product.IsActive = !product.IsActive;
    
    // 4. Lưu thay đổi vào database
    await _db.SaveChangesAsync();

    // 5. Trả về JSON response
    return Json(new { 
        success = true, 
        message = product.IsActive ? "Đã kích hoạt sản phẩm" : "Đã vô hiệu hóa sản phẩm",
        isActive = product.IsActive
    });
}
```

**Giải thích từng bước:**
- `[HttpPost]` - Chỉ nhận POST request
- `[ValidateAntiForgeryToken]` - Xác thực token để chống CSRF attack
- `product.IsActive = !product.IsActive` - Đảo ngược giá trị boolean:
  - `true` → `false` (Tắt sản phẩm)
  - `false` → `true` (Bật sản phẩm)
- `_db.SaveChangesAsync()` - Lưu thay đổi vào database

---

## 5️⃣ **Nơi sử dụng IsActive trong hệ thống**

### a) ProductService.cs - Lọc sản phẩm hiển thị
```csharp
// Chỉ lấy sản phẩm đang active
.Where(p => p.IsActive)
```

**Các method:**
- `GetFeaturedProducts()` - Sản phẩm nổi bật
- `GetFilteredProducts()` - Sản phẩm trong trang sản phẩm
- `GetCategoryProductCounts()` - Đếm sản phẩm theo danh mục

### b) AiController.cs - AI Chatbot
Sử dụng `IsActive` để chỉ đề xuất sản phẩm đang bán cho chatbot.

### c) Views - Hiển thị trạng thái
- `Products.cshtml` - Admin quản lý sản phẩm
- `Employees.cshtml` - Hiển thị trạng thái nhân viên (dùng IsActive tương tự)

---

## 6️⃣ **Luồng hoạt động tổng thể**

```
┌─────────────────┐
│ User click nút  │
│ "Bật" hoặc "Tắt"│
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ toggleProductStatus()   │
│ (JavaScript)            │
│ - Xác nhận với user     │
│ - Lấy Anti-Forgery Token│
└────────┬────────────────┘
         │
         ▼ POST request
┌─────────────────────────┐
│ ToggleProductStatus()   │
│ (Controller)            │
│ - Validate quyền         │
│ - Tìm product trong DB  │
│ - Đảo ngược IsActive    │
│ - SaveChanges()         │
└────────┬────────────────┘
         │
         ▼ JSON response
┌─────────────────────────┐
│ JavaScript nhận response│
│ - Hiển thị thông báo    │
│ - Reload trang          │
└─────────────────────────┘
```

---

## 7️⃣ **Bảo mật**

### Anti-Forgery Token
- Ngăn chặn CSRF (Cross-Site Request Forgery) attacks
- Token được tạo trong view: `@Html.AntiForgeryToken()`
- Server validate: `[ValidateAntiForgeryToken]`

### Authorization
- Kiểm tra `CurrentEmpId` - Chỉ admin mới có quyền
- `[Authorize(AuthenticationSchemes = "AdminScheme")]` ở controller level

---

## 8️⃣ **Tác động khi toggle**

### Khi BẬT (IsActive = true):
✅ Sản phẩm xuất hiện trên website
✅ Khách hàng có thể mua
✅ Hiển thị trong danh sách sản phẩm
✅ AI chatbot có thể đề xuất

### Khi TẮT (IsActive = false):
❌ Sản phẩm KHÔNG hiển thị trên website
❌ Khách hàng KHÔNG thể mua
❌ Ẩn khỏi danh sách sản phẩm
❌ AI chatbot KHÔNG đề xuất

**Lưu ý:** Sản phẩm vẫn còn trong database, chỉ bị ẩn khỏi frontend.

---

## 9️⃣ **Các file liên quan**

### Core Files:
1. `Models/Products/Product.cs` - Định nghĩa IsActive
2. `Controllers/AdminController.cs` - Action ToggleProductStatus
3. `Views/Admin/Products.cshtml` - UI và JavaScript

### Files sử dụng IsActive:
1. `Services/Implementations/ECommerce/ProductService.cs` - Lọc sản phẩm
2. `Controllers/AiController.cs` - AI recommendations
3. `Controllers/ProductController.cs` - Hiển thị sản phẩm cho customer

---

## 🔟 **Ví dụ thực tế**

**Trường hợp 1: Sản phẩm hết hàng tạm thời**
- Admin click "Tắt" → `IsActive = false`
- Sản phẩm biến mất khỏi website
- Khi có hàng lại, click "Bật" → `IsActive = true`
- Sản phẩm xuất hiện lại

**Trường hợp 2: Sản phẩm ngừng kinh doanh**
- Admin click "Tắt" → `IsActive = false`
- Sản phẩm không hiển thị
- Dữ liệu vẫn lưu trong DB để thống kê

---

## 📝 Tóm tắt

**Tính năng này cho phép:**
- ✅ Bật/tắt sản phẩm nhanh chóng mà không cần xóa
- ✅ Ẩn sản phẩm khỏi website tạm thời
- ✅ Giữ lại dữ liệu trong database để thống kê
- ✅ Bảo mật với Anti-Forgery Token
- ✅ Chỉ admin mới có quyền thực hiện
















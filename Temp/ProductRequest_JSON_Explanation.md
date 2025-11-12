# Cách Admin Đọc JSON ProductSizesJson và Thêm Món Vào Hệ Thống

## 📋 Tổng Quan

Khi Admin duyệt yêu cầu thêm/sửa sản phẩm, hệ thống sẽ:
1. **Đọc JSON** từ trường `ProductSizesJson` trong bảng `ProductRequest`
2. **Parse JSON** thành danh sách các size và giá
3. **Tạo ProductSizes** trong database

---

## 🔍 Đoạn Code Chính

### 1. Helper Class để Parse JSON

**File:** `Controllers/AdminController.cs` (dòng 2293-2298)

```csharp
// Helper class để deserialize ProductSizes JSON
private class ProductSizeData
{
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

**Giải thích:** Class này dùng để map JSON thành object C#. JSON có format:
```json
[
    {"Size":"S","Price":25000},
    {"Size":"M","Price":30000},
    {"Size":"L","Price":35000}
]
```

---

### 2. Đoạn Code Đọc JSON và Tạo ProductSizes (Khi Thêm Mới)

**File:** `Controllers/AdminController.cs` (dòng 2154-2183)

```csharp
// Thêm ProductSizes nếu có
if (!string.IsNullOrEmpty(request.ProductSizesJson))
{
    try
    {
        // BƯỚC 1: Parse JSON thành List<ProductSizeData>
        var sizes = System.Text.Json.JsonSerializer.Deserialize<List<ProductSizeData>>(request.ProductSizesJson);
        
        if (sizes != null && sizes.Count > 0)
        {
            // BƯỚC 2: Duyệt qua từng size trong JSON
            foreach (var sizeData in sizes)
            {
                // BƯỚC 3: Kiểm tra size và giá hợp lệ
                if (!string.IsNullOrWhiteSpace(sizeData.Size) && sizeData.Price > 0)
                {
                    // BƯỚC 4: Tạo ProductSize mới
                    var productSize = new ProductSize
                    {
                        ProductID = product.ProductID,  // ID của Product vừa tạo
                        Size = sizeData.Size.Trim().ToUpper(),  // Chuyển thành chữ hoa (S, M, L)
                        Price = sizeData.Price  // Giá từ JSON
                    };
                    
                    // BƯỚC 5: Thêm vào database
                    _db.ProductSizes.Add(productSize);
                }
            }
            // BƯỚC 6: Lưu tất cả vào database
            await _db.SaveChangesAsync();
        }
    }
    catch (Exception jsonEx)
    {
        // Log lỗi nếu JSON không hợp lệ (nhưng không fail toàn bộ request)
        System.Diagnostics.Debug.WriteLine("Error parsing ProductSizesJson: " + jsonEx.Message);
    }
}
```

---

### 3. Đoạn Code Đọc JSON và Cập Nhật ProductSizes (Khi Sửa)

**File:** `Controllers/AdminController.cs` (dòng 2201-2231)

```csharp
// Xóa sizes cũ và thêm sizes mới
_db.ProductSizes.RemoveRange(product.ProductSizes);

// Thêm ProductSizes mới nếu có
if (!string.IsNullOrEmpty(request.ProductSizesJson))
{
    try
    {
        // Parse JSON
        var sizes = System.Text.Json.JsonSerializer.Deserialize<List<ProductSizeData>>(request.ProductSizesJson);
        
        if (sizes != null && sizes.Count > 0)
        {
            foreach (var sizeData in sizes)
            {
                if (!string.IsNullOrWhiteSpace(sizeData.Size) && sizeData.Price > 0)
                {
                    var productSize = new ProductSize
                    {
                        ProductID = product.ProductID,
                        Size = sizeData.Size.Trim().ToUpper(),
                        Price = sizeData.Price
                    };
                    _db.ProductSizes.Add(productSize);
                }
            }
        }
    }
    catch (Exception jsonEx)
    {
        System.Diagnostics.Debug.WriteLine("Error parsing ProductSizesJson: " + jsonEx.Message);
    }
}
```

**Lưu ý:** Khi sửa, hệ thống sẽ:
1. Xóa tất cả ProductSizes cũ
2. Thêm lại ProductSizes mới từ JSON

---

## 📊 Ví Dụ Cụ Thể

### JSON Input (từ database):
```json
[
    {"Size":"S","Price":25000},
    {"Size":"M","Price":30000},
    {"Size":"L","Price":35000}
]
```

### Sau khi Parse:
```csharp
sizes = [
    { Size = "S", Price = 25000 },
    { Size = "M", Price = 30000 },
    { Size = "L", Price = 35000 }
]
```

### Kết quả trong Database (bảng ProductSize):
| ProductSizeID | ProductID | Size | Price |
|--------------|-----------|------|-------|
| 1            | 10        | S    | 25000 |
| 2            | 10        | M    | 30000 |
| 3            | 10        | L    | 35000 |

---

## 🎯 Luồng Hoạt Động Khi Admin Duyệt

```
1. Admin click "Duyệt" trên yêu cầu
   ↓
2. ApproveProductRequest() được gọi
   ↓
3. Tạo Product mới (lấy ProductID)
   ↓
4. Kiểm tra ProductSizesJson có dữ liệu không?
   ↓
5. Nếu có → Parse JSON thành List<ProductSizeData>
   ↓
6. Duyệt qua từng size trong JSON
   ↓
7. Tạo ProductSize và thêm vào database
   ↓
8. SaveChanges() → Lưu tất cả vào database
   ↓
9. Trả về success message
```

---

## 🔧 Xử Lý Lỗi

Nếu JSON không hợp lệ hoặc parse lỗi:
- Hệ thống sẽ **log lỗi** nhưng **không fail** toàn bộ request
- Product vẫn được tạo, chỉ không có ProductSizes
- Admin có thể thêm ProductSizes sau bằng cách sửa sản phẩm

---

## 📝 Code Hiển Thị JSON Trong View

**File:** `Views/Admin/ViewProductApproval.cshtml` (dòng 16-25)

```csharp
// Parse ProductSizes JSON để hiển thị
List<ProductSizeDisplay>? productSizes = null;
if (!string.IsNullOrEmpty(Model.ProductSizesJson))
{
    try
    {
        productSizes = System.Text.Json.JsonSerializer.Deserialize<List<ProductSizeDisplay>>(Model.ProductSizesJson);
    }
    catch { }
}
```

Sau đó hiển thị trong bảng:
```html
@if (productSizes != null && productSizes.Count > 0)
{
    <table>
        @foreach (var size in productSizes)
        {
            <tr>
                <td>@size.Size</td>
                <td>@size.Price.ToString("N0") đ</td>
            </tr>
        }
    </table>
}
```

---

## ✅ Tóm Tắt

**Câu hỏi:** Làm sao admin đọc được JSON và thêm món?

**Trả lời:**
1. JSON được lưu trong trường `ProductSizesJson` của bảng `ProductRequest`
2. Khi duyệt, code dùng `JsonSerializer.Deserialize<List<ProductSizeData>>()` để parse
3. Duyệt qua từng phần tử trong list và tạo `ProductSize`
4. Lưu vào database bằng `_db.ProductSizes.Add()` và `SaveChangesAsync()`

**Đoạn code chính:** Dòng 2159 trong `AdminController.cs`



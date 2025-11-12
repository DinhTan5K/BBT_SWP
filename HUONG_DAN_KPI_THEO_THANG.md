# Hướng dẫn: Cách KPI hoạt động theo tháng

## 📅 Cách KPI tính theo tháng

### 1. KPI tính dựa trên `RequestedAt` (Ngày tạo request)

**Code tính KPI:**
```csharp
// File: Services/Employee/MarketingKPIService.cs
// Dòng 21-38

public async Task<MarketingKPIVm?> CalculateKPIAsync(string employeeId, int year, int month)
{
    // Tính ngày bắt đầu và kết thúc của tháng
    var startDate = new DateTime(year, month, 1);        // VD: 2024-12-01 00:00:00
    var endDate = startDate.AddMonths(1).AddDays(-1);   // VD: 2024-12-31 23:59:59

    // Lấy News Requests TRONG THÁNG ĐÓ
    var newsRequests = await _db.NewsRequests
        .Where(nr => nr.RequestedBy == employeeId 
            && nr.RequestedAt >= startDate      // >= 2024-12-01
            && nr.RequestedAt <= endDate)       // <= 2024-12-31
        .ToListAsync();

    // Tương tự với Discount Requests
    var discountRequests = await _db.DiscountRequests
        .Where(dr => dr.RequestedBy == employeeId 
            && dr.RequestedAt >= startDate 
            && dr.RequestedAt <= endDate)
        .ToListAsync();
}
```

### 2. Điều kiện quan trọng

✅ **KPI chỉ tính các requests có `RequestedAt` trong tháng được chọn**

Ví dụ:
- Nếu bạn chọn xem KPI **tháng 12/2024**
- Chỉ tính các requests có `RequestedAt` từ **2024-12-01** đến **2024-12-31**
- Requests tháng 11 hoặc tháng 1 sẽ KHÔNG được tính

---

## 🔍 Tại sao KPI không hiển thị sau khi insert data?

### Nguyên nhân có thể:

#### 1. **Bạn chưa vào trang KPI để trigger tính toán**

KPI **KHÔNG tự động tính** khi insert data. Bạn cần:

**Cách 1: Vào trang KPI**
1. Đăng nhập với tài khoản Marketing
2. Vào **"KPI & Lương"** trong menu
3. Chọn **tháng 12** và **năm 2024**
4. Hệ thống sẽ tự động tính và lưu KPI

**Cách 2: Vào trang Lương**
1. Đăng nhập với tài khoản Marketing
2. Vào **"Lương"** trong menu
3. Chọn **tháng 12** và **năm 2024**
4. Hệ thống sẽ tự động tính KPI và hiển thị bonus

#### 2. **RequestedAt trong database không đúng tháng 12**

Kiểm tra SQL:
```sql
-- Kiểm tra xem requests có đúng tháng 12 không
SELECT 
    Id,
    RequestedBy,
    RequestedAt,
    Status,
    MONTH(RequestedAt) as Month,
    YEAR(RequestedAt) as Year
FROM NewsRequest
WHERE RequestedBy = 'MK001'  -- Thay bằng mã nhân viên Marketing của bạn
ORDER BY RequestedAt DESC;

SELECT 
    Id,
    RequestedBy,
    RequestedAt,
    Status,
    MONTH(RequestedAt) as Month,
    YEAR(RequestedAt) as Year
FROM DiscountRequest
WHERE RequestedBy = 'MK001'  -- Thay bằng mã nhân viên Marketing của bạn
ORDER BY RequestedAt DESC;
```

**Nếu `Month` không phải là 12**, thì KPI sẽ không tính được.

#### 3. **Bạn đang xem KPI tháng khác**

Khi vào trang KPI, mặc định sẽ hiển thị **tháng hiện tại**. Nếu bạn insert data tháng 12 nhưng đang ở tháng 1, bạn cần:

1. Vào trang KPI
2. Click nút **"Tháng trước"** hoặc chọn tháng 12 từ dropdown
3. Hoặc truy cập trực tiếp: `/Employee/MarketingKPI?year=2024&month=12`

---

## ✅ Cách test KPI đúng cách

### Bước 1: Insert test data
```sql
-- Chạy script Insert_TestData_For_KPI.sql
-- Đảm bảo @TestMonth = 12 và @TestYear = 2024
```

### Bước 2: Kiểm tra data đã insert đúng chưa
```sql
-- Đếm số requests trong tháng 12
SELECT 
    COUNT(*) as TotalNewsRequests,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as Approved,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as Rejected,
    SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as Pending
FROM NewsRequest
WHERE RequestedBy = 'MK001'  -- Thay bằng mã nhân viên Marketing
  AND YEAR(RequestedAt) = 2024
  AND MONTH(RequestedAt) = 12;

SELECT 
    COUNT(*) as TotalDiscountRequests,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as Approved,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as Rejected,
    SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as Pending
FROM DiscountRequest
WHERE RequestedBy = 'MK001'  -- Thay bằng mã nhân viên Marketing
  AND YEAR(RequestedAt) = 2024
  AND MONTH(RequestedAt) = 12;
```

### Bước 3: Trigger tính KPI
1. Đăng nhập với tài khoản Marketing
2. Vào: `http://localhost:5098/Employee/MarketingKPI?year=2024&month=12`
3. Hoặc vào menu **"KPI & Lương"** và chọn tháng 12

### Bước 4: Kiểm tra KPI đã được lưu chưa
```sql
-- Kiểm tra KPI đã được tính và lưu chưa
SELECT 
    EmployeeID,
    KpiMonth,
    TotalNewsRequests,
    TotalDiscountRequests,
    KPIScore,
    IsKPIAchieved,
    KPIBonus,
    CreatedAt
FROM MarketingKPI
WHERE EmployeeID = 'MK001'  -- Thay bằng mã nhân viên Marketing
  AND YEAR(KpiMonth) = 2024
  AND MONTH(KpiMonth) = 12;
```

---

## 🎯 Luồng hoạt động KPI

```
1. Insert NewsRequest/DiscountRequest vào database
   └─> RequestedAt = 2024-12-15 (ví dụ)

2. User vào trang KPI và chọn tháng 12/2024
   └─> Gọi CalculateAndSaveKPIAsync('MK001', 2024, 12)

3. System tính KPI:
   ├─> Lọc requests: RequestedAt >= 2024-12-01 AND <= 2024-12-31
   ├─> Đếm Approved/Rejected/Pending
   ├─> Tính NewsApproveRate, DiscountApproveRate, OverallApproveRate
   ├─> Tính KPIScore (dựa trên công thức)
   └─> Tính KPIBonus (dựa trên KPIScore và BaseSalary)

4. Lưu vào bảng MarketingKPI:
   └─> INSERT hoặc UPDATE record với KpiMonth = 2024-12-01

5. Hiển thị trên UI
```

---

## ⚠️ Lưu ý quan trọng

1. **KPI tính theo `RequestedAt`, KHÔNG phải `ReviewedAt`**
   - Dù request được duyệt vào tháng 1, nhưng nếu `RequestedAt` là tháng 12, nó vẫn tính vào KPI tháng 12

2. **KPI không tự động cập nhật**
   - Mỗi lần vào trang KPI, hệ thống sẽ tính lại và cập nhật
   - Nếu có request mới, cần vào lại trang KPI để tính lại

3. **Mỗi tháng có 1 KPI record duy nhất**
   - Bảng MarketingKPI có unique constraint: (EmployeeID, KpiMonth)
   - Nếu tính lại, sẽ UPDATE record cũ thay vì tạo mới

4. **KPI chỉ tính cho Marketing employees (RoleID = 'MK')**
   - Nếu employee không phải MK, KPI sẽ không được tính

---

## 🐛 Debug nếu KPI vẫn không hiển thị

### Kiểm tra 1: Employee có phải Marketing không?
```sql
SELECT EmployeeID, FullName, RoleID
FROM Employee
WHERE EmployeeID = 'MK001';  -- Phải có RoleID = 'MK'
```

### Kiểm tra 2: Có requests trong tháng 12 không?
```sql
SELECT COUNT(*) 
FROM NewsRequest 
WHERE RequestedBy = 'MK001' 
  AND YEAR(RequestedAt) = 2024 
  AND MONTH(RequestedAt) = 12;
```

### Kiểm tra 3: Có Contract để tính BaseSalary không?
```sql
SELECT * 
FROM Contract 
WHERE EmployeeId = 'MK001' 
  AND Status = 'Hiệu lực';
```

### Kiểm tra 4: Log trong code
Thêm breakpoint hoặc log vào:
- `MarketingKPIService.CalculateKPIAsync` (dòng 21)
- Kiểm tra xem `newsRequests.Count` và `discountRequests.Count` có > 0 không

---

## 📝 Tóm tắt

**KPI hoạt động theo tháng dựa trên:**
- ✅ `RequestedAt` của NewsRequest và DiscountRequest
- ✅ Phải vào trang KPI hoặc Lương để trigger tính toán
- ✅ Phải chọn đúng tháng/năm khi xem KPI
- ✅ Phải có Contract để tính BaseSalary (cho bonus)

**Nếu KPI không hiển thị:**
1. Kiểm tra `RequestedAt` có đúng tháng không
2. Vào trang KPI và chọn đúng tháng/năm
3. Kiểm tra employee có RoleID = 'MK' không
4. Kiểm tra có Contract không


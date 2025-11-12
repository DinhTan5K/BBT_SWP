# LUỒNG XỬ LÝ REQUEST THÊM BRANCH (CHI NHÁNH)

## 📋 TỔNG QUAN

Việc thêm branch vào hệ thống là một thao tác quan trọng, ảnh hưởng đến:
- Cấu trúc tổ chức (Region → Branch)
- Quản lý nhân viên (Employee → Branch)
- Quản lý đơn hàng (Order → Branch)
- Hệ thống phân quyền và quản lý

## 🔄 LUỒNG XỬ LÝ CHÍNH

### 1. **TẠO REQUEST (Region Manager - RM)**

**Người thực hiện:** Region Manager (RM)  
**Vị trí:** Trang quản lý Region/Branch của RM

**Thông tin cần thu thập:**
- **Tên chi nhánh** (Name) - Bắt buộc
- **Địa chỉ** (Address) - Bắt buộc
- **Số điện thoại** (Phone) - Bắt buộc
- **Thành phố** (City) - Bắt buộc
- **Region** (RegionID) - Bắt buộc (RM chỉ có thể tạo branch trong region của mình)
- **Tọa độ GPS** (Latitude, Longitude) - Khuyến khích (có thể dùng map picker)
- **Ghi chú/Lý do** (Optional) - Giải thích tại sao cần thêm branch này

**Validation:**
- Kiểm tra tên branch không trùng trong cùng region
- Kiểm tra địa chỉ hợp lệ
- Kiểm tra số điện thoại format
- Kiểm tra tọa độ GPS nếu có

**UX/UI:**
- Form có validation real-time
- Map picker để chọn vị trí (Google Maps/OpenStreetMap)
- Preview thông tin trước khi submit
- Hiển thị danh sách branches hiện có trong region để tránh trùng

---

### 2. **LƯU REQUEST VÀO DATABASE**

**Model: BranchRequest**
- Tương tự CategoryRequest, ProductRequest
- Lưu tất cả thông tin branch vào request table
- Status = Pending
- RequestedBy = RM EmployeeID
- RequestedAt = DateTime.UtcNow

**Lưu ý:**
- Branch chưa được tạo trong bảng Branch
- Chỉ tạo khi Admin approve

---

### 3. **ADMIN XEM DANH SÁCH REQUEST**

**Vị trí:** `/Admin/Approvals` (trang chung cho tất cả requests)

**Hiển thị:**
- Danh sách BranchRequest cùng với các request khác (Category, Product, Discount, etc.)
- Filter theo:
  - Status (Pending, Approved, Rejected)
  - Request Type (Add, Edit, Delete)
  - Region (nếu cần)
- Sort: Pending trước, sau đó theo thời gian (mới nhất trước)
- Pagination

**Thông tin hiển thị trong bảng:**
- ID Request
- Loại: "Thêm chi nhánh" / "Sửa chi nhánh" / "Xóa chi nhánh"
- Tên Branch
- Region
- Người yêu cầu (RM name)
- Thời gian yêu cầu
- Status badge (màu sắc: vàng=Pending, xanh=Approved, đỏ=Rejected)
- Actions: Duyệt / Từ chối / Chi tiết

---

### 4. **ADMIN XEM CHI TIẾT REQUEST**

**Vị trí:** `/Admin/ViewApproval/{id}?type=branch`

**Thông tin hiển thị:**

**Phần 1: Thông tin Request**
- Request ID
- Loại request (Add/Edit/Delete)
- Status
- Người yêu cầu (RM): Tên, EmployeeID, Region
- Thời gian yêu cầu
- Người duyệt (nếu đã duyệt): Tên, EmployeeID
- Thời gian duyệt (nếu đã duyệt)
- Lý do từ chối (nếu bị reject)

**Phần 2: Thông tin Branch**
- **Nếu là Add:**
  - Tên branch (mới)
  - Địa chỉ
  - Số điện thoại
  - Thành phố
  - Region (hiển thị tên region)
  - Tọa độ GPS (hiển thị trên map nếu có)
  - Ghi chú/Lý do (nếu có)
  
- **Nếu là Edit:**
  - Thông tin cũ (từ Branch hiện tại)
  - Thông tin mới (từ request)
  - So sánh side-by-side (highlight thay đổi)
  
- **Nếu là Delete:**
  - Thông tin branch sẽ bị xóa
  - Cảnh báo: Số nhân viên, số đơn hàng liên quan
  - Yêu cầu xác nhận kỹ

**Phần 3: Thông tin liên quan**
- Số nhân viên hiện tại trong branch (nếu Edit/Delete)
- Số đơn hàng liên quan (nếu Edit/Delete)
- Cảnh báo nếu có ràng buộc dữ liệu

**UX/UI:**
- Layout rõ ràng, dễ đọc
- Map hiển thị vị trí branch (nếu có GPS)
- Highlight các thay đổi (nếu Edit)
- Cảnh báo màu đỏ nếu có rủi ro (xóa branch có nhiều dữ liệu)

---

### 5. **ADMIN DUYỆT REQUEST (APPROVE)**

**Khi Admin click "Duyệt":**

**Xử lý:**
1. Validate lại thông tin:
   - Tên branch không trùng trong cùng region
   - RegionID tồn tại
   - Địa chỉ hợp lệ
   - Số điện thoại hợp lệ

2. **Nếu RequestType = Add:**
   - Tạo Branch mới với thông tin từ request
   - Tự động generate BranchID (tăng dần)
   - Set IsActive = true (hoặc có thể để admin chọn)
   - Lưu vào bảng Branch

3. **Nếu RequestType = Edit:**
   - Tìm Branch theo BranchId trong request
   - Cập nhật thông tin từ request
   - Kiểm tra không có conflict (ví dụ: tên trùng với branch khác)

4. **Nếu RequestType = Delete:**
   - Kiểm tra ràng buộc:
     - Có nhân viên nào trong branch không? (cảnh báo)
     - Có đơn hàng nào liên quan không? (cảnh báo)
   - Có 2 options:
     - **Soft Delete:** Set IsActive = false (khuyến nghị)
     - **Hard Delete:** Xóa khỏi database (nguy hiểm, cần xác nhận kỹ)

5. Cập nhật BranchRequest:
   - Status = Approved
   - ReviewedBy = Admin EmployeeID
   - ReviewedAt = DateTime.UtcNow

6. **Notification (nếu có hệ thống thông báo):**
   - Gửi thông báo cho RM: "Yêu cầu thêm branch [Tên] đã được duyệt"

**Response:**
- Success: "Đã duyệt yêu cầu thành công!"
- Error: Hiển thị lỗi cụ thể

---

### 6. **ADMIN TỪ CHỐI REQUEST (REJECT)**

**Khi Admin click "Từ chối":**

**Xử lý:**
1. Hiển thị modal/form nhập lý do từ chối (bắt buộc)
2. Validate: Lý do không được để trống (tối thiểu 10 ký tự)
3. Cập nhật BranchRequest:
   - Status = Rejected
   - ReviewedBy = Admin EmployeeID
   - ReviewedAt = DateTime.UtcNow
   - RejectionReason = Lý do từ chối

4. **Notification (nếu có):**
   - Gửi thông báo cho RM: "Yêu cầu thêm branch [Tên] đã bị từ chối. Lý do: [Lý do]"

**Response:**
- Success: "Đã từ chối yêu cầu"
- Error: Hiển thị lỗi

---

## 🎨 UX/UI DESIGN

### **Trang tạo Request (RM Side)**

**Layout:**
```
┌─────────────────────────────────────────┐
│  TẠO YÊU CẦU THÊM CHI NHÁNH            │
├─────────────────────────────────────────┤
│                                         │
│  Tên chi nhánh *                        │
│  [___________________________]          │
│                                         │
│  Region *                               │
│  [Dropdown: Region của RM]             │
│                                         │
│  Địa chỉ *                              │
│  [___________________________]          │
│                                         │
│  Thành phố *                            │
│  [___________________________]          │
│                                         │
│  Số điện thoại *                        │
│  [___________________________]          │
│                                         │
│  Tọa độ GPS (khuyến khích)             │
│  [Map Picker]                           │
│  Latitude: [___] Longitude: [___]      │
│                                         │
│  Ghi chú                                │
│  [Textarea: Lý do cần thêm branch...]  │
│                                         │
│  [Preview] [Hủy] [Gửi yêu cầu]         │
└─────────────────────────────────────────┘
```

**Features:**
- Real-time validation
- Map picker để chọn vị trí
- Preview trước khi submit
- Hiển thị danh sách branches hiện có trong region

---

### **Trang danh sách Request (Admin Side)**

**Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│  QUẢN LÝ YÊU CẦU - BRANCH REQUESTS                         │
├─────────────────────────────────────────────────────────────┤
│  Filter: [Status ▼] [Request Type ▼] [Region ▼] [Search]   │
├─────────────────────────────────────────────────────────────┤
│ ID │ Loại      │ Tên Branch │ Region │ RM │ Thời gian │ STT│
├────┼───────────┼────────────┼────────┼────┼───────────┼────┤
│ 1  │ Thêm mới  │ CN Hà Nội  │ Miền Bắc│RM1│ 01/01/24 │ 🟡 │
│    │           │            │        │    │          │[Duyệt][Từ chối][Chi tiết]│
└────┴───────────┴────────────┴────────┴────┴───────────┴────┘
```

---

### **Trang chi tiết Request (Admin Side)**

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│  CHI TIẾT YÊU CẦU THÊM CHI NHÁNH - #123                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📋 THÔNG TIN REQUEST                                   │
│  ─────────────────────────────────────                  │
│  Loại: Thêm mới                                         │
│  Status: 🟡 Chờ duyệt                                    │
│  Người yêu cầu: Nguyễn Văn A (RM001)                   │
│  Thời gian: 01/01/2024 10:30 AM                        │
│                                                         │
│  🏢 THÔNG TIN CHI NHÁNH                                 │
│  ─────────────────────────────────────                  │
│  Tên: CN Hà Nội                                         │
│  Địa chỉ: 123 Đường ABC, Quận XYZ                      │
│  Thành phố: Hà Nội                                      │
│  Số điện thoại: 0123456789                              │
│  Region: Miền Bắc                                        │
│  GPS: 21.0285, 105.8542                                 │
│  [Map hiển thị vị trí]                                  │
│                                                         │
│  📝 Ghi chú:                                            │
│  Cần mở thêm chi nhánh để phục vụ khu vực mới...        │
│                                                         │
│  [Duyệt] [Từ chối] [Quay lại]                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🔐 PHÂN QUYỀN

- **Region Manager (RM):**
  - Tạo request thêm branch (chỉ trong region của mình)
  - Xem danh sách requests của mình
  - Xem chi tiết request của mình
  - Không thể duyệt/từ chối

- **Admin:**
  - Xem tất cả requests
  - Xem chi tiết request
  - Duyệt/Từ chối request
  - Có thể tạo branch trực tiếp (nếu cần, bypass request)

---

## ⚠️ CÁC TRƯỜNG HỢP ĐẶC BIỆT

### 1. **Tên Branch Trùng**
- Kiểm tra trùng trong cùng region
- Nếu trùng: Từ chối hoặc yêu cầu đổi tên

### 2. **Edit Branch có nhiều dữ liệu**
- Cảnh báo số nhân viên, đơn hàng
- Cho phép admin xác nhận lại

### 3. **Delete Branch có dữ liệu**
- Không cho phép hard delete nếu có:
  - Nhân viên trong branch
  - Đơn hàng liên quan
- Chỉ cho phép soft delete (IsActive = false)

### 4. **RM tạo request nhưng sau đó bị thay đổi region**
- Giữ nguyên request (historical data)
- Admin cần kiểm tra kỹ khi duyệt

### 5. **Request bị reject, RM muốn tạo lại**
- Cho phép tạo request mới với thông tin đã chỉnh sửa
- Hiển thị lý do từ chối của request cũ để tham khảo

---

## 📊 DATABASE DESIGN

### **BranchRequest Table**

```sql
CREATE TABLE BranchRequest (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Loại yêu cầu: 0 = Add, 1 = Edit, 2 = Delete
    RequestType INT NOT NULL DEFAULT 0,
    
    -- ID của Branch nếu là Edit hoặc Delete (NULL nếu là Add)
    BranchId INT NULL,
    
    -- Thông tin người yêu cầu (RM)
    RequestedBy VARCHAR(10) NOT NULL,
    RequestedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    
    -- Thông tin duyệt
    Status INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
    ReviewedBy VARCHAR(10) NULL, -- Admin ID
    ReviewedAt DATETIME2(7) NULL,
    RejectionReason NVARCHAR(500) NULL,
    
    -- Dữ liệu Branch (lưu tất cả thông tin)
    Name NVARCHAR(255) NOT NULL,
    Address NVARCHAR(500) NULL,
    Phone VARCHAR(20) NULL,
    RegionID INT NOT NULL,
    City NVARCHAR(100) NULL,
    Latitude DECIMAL(18,15) NULL,
    Longitude DECIMAL(18,15) NULL,
    
    -- Ghi chú/Lý do
    Notes NVARCHAR(1000) NULL,
    
    -- Foreign Keys
    FOREIGN KEY (RequestedBy) REFERENCES Employee(EmployeeID),
    FOREIGN KEY (ReviewedBy) REFERENCES Employee(EmployeeID),
    FOREIGN KEY (BranchId) REFERENCES Branch(BranchID),
    FOREIGN KEY (RegionID) REFERENCES Region(RegionID)
);
```

---

## 🔄 WORKFLOW SUMMARY

```
RM Tạo Request
    ↓
Lưu vào BranchRequest (Status = Pending)
    ↓
Admin xem danh sách requests
    ↓
Admin xem chi tiết request
    ↓
Admin quyết định:
    ├─→ Duyệt (Approve)
    │       ↓
    │   Tạo/Cập nhật Branch
    │       ↓
    │   Cập nhật Status = Approved
    │       ↓
    │   Thông báo cho RM
    │
    └─→ Từ chối (Reject)
            ↓
        Cập nhật Status = Rejected
            ↓
        Lưu lý do từ chối
            ↓
        Thông báo cho RM
```

---

## ✅ CHECKLIST KHI IMPLEMENT

- [ ] Tạo BranchRequest model
- [ ] Tạo BranchRequest table (SQL)
- [ ] Tạo form tạo request (RM side)
- [ ] Validation form
- [ ] API endpoint tạo request
- [ ] Hiển thị trong Admin/Approvals
- [ ] Trang chi tiết request
- [ ] Logic approve (tạo branch)
- [ ] Logic reject (lưu lý do)
- [ ] Notification (nếu có)
- [ ] Test các trường hợp edge case
- [ ] Phân quyền (RM chỉ tạo, Admin duyệt)

---

## 💡 KHUYẾN NGHỊ

1. **Map Integration:** Nên tích hợp map picker để chọn vị trí chính xác
2. **Preview:** Cho RM xem preview trước khi submit
3. **History:** Lưu lịch sử thay đổi (audit log)
4. **Notification:** Thông báo real-time khi request được duyệt/từ chối
5. **Bulk Actions:** Nếu cần, cho phép admin duyệt nhiều requests cùng lúc
6. **Export:** Cho phép export danh sách requests ra Excel

---

**Tài liệu này mô tả đầy đủ luồng xử lý request thêm branch. Khi implement, cần tuân thủ pattern hiện có của CategoryRequest và ProductRequest để đảm bảo tính nhất quán.**



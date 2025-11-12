# Hướng dẫn Test KPI Marketing và Tích hợp Lương

## Các bước test:

### 1. Chuẩn bị dữ liệu

#### A. Tạo Contract cho Marketing Employee
1. Đăng nhập với tài khoản Admin
2. Vào **Quản lý nhân viên** → Chọn Marketing employee
3. Nhấn nút **"Tạo hợp đồng"**
4. Điền thông tin:
   - Số hợp đồng: VD: `HD-2024-001`
   - Loại hợp đồng: Chọn một trong các loại
   - Ngày bắt đầu: Chọn ngày
   - Cách tính lương: **"Tháng"** hoặc **"Giờ"**
   - Mức lương: VD: `10,000,000` (nếu chọn "Tháng") hoặc `50,000` (nếu chọn "Giờ")
   - Trạng thái: **"Hiệu lực"**

#### B. Tạo News và Discount Requests

**Cách 1: Sử dụng SQL Script (Khuyến nghị - Nhanh hơn)**

1. Mở file `SQL/Insert_TestData_For_KPI.sql` trong SQL Server Management Studio
2. Thay đổi các biến ở đầu script:
   ```sql
   DECLARE @MarketingEmployeeID VARCHAR(10) = 'MK001'; -- Thay bằng mã nhân viên Marketing thực tế
   DECLARE @AdminEmployeeID VARCHAR(10) = 'AD001';     -- Thay bằng mã nhân viên Admin thực tế
   DECLARE @TestMonth INT = 12;                        -- Tháng test (1-12)
   DECLARE @TestYear INT = 2024;                      -- Năm test
   ```
3. Chạy script để tự động insert:
   - **15 News Requests**: 10 Approved, 2 Rejected, 3 Pending
   - **10 Discount Requests**: 7 Approved, 1 Rejected, 2 Pending
4. Script sẽ tự động tính toán và hiển thị KPI dự kiến

**Cách 2: Tạo thủ công qua UI**

1. Đăng nhập với tài khoản Marketing (MK)
2. Tạo một số News requests:
   - Vào **"Tạo yêu cầu tin tức"**
   - Tạo ít nhất 10-15 requests
3. Tạo một số Discount requests:
   - Vào **"Tạo yêu cầu mã giảm giá"**
   - Tạo ít nhất 5-10 requests

#### C. Admin duyệt một số requests

**Nếu dùng SQL Script (Cách 1)**: Bỏ qua bước này vì script đã tự động set status Approved/Rejected/Pending.

**Nếu tạo thủ công (Cách 2)**:
1. Đăng nhập với tài khoản Admin
2. Vào **"Duyệt yêu cầu"** (News và Discount)
3. Duyệt một số requests:
   - **Approved**: Ít nhất 10-15 requests (để đạt KPI)
   - **Rejected**: Ít nhất 1-2 requests (để test reject rate)
   - **Pending**: Có thể để một số requests pending

### 2. Test KPI Calculation

#### A. Xem KPI Dashboard
1. Đăng nhập với tài khoản Marketing
2. Vào **"KPI & Lương"** trong menu
3. Chọn tháng/năm cần xem
4. Kiểm tra:
   - **Tổng số requests**: News + Discount
   - **Số requests đã duyệt**: Approved
   - **Số requests bị từ chối**: Rejected
   - **Tỷ lệ duyệt**: Approve Rate
   - **Điểm KPI**: Total Score (tối đa 100 điểm)
   - **Trạng thái**: Đạt KPI (>= 70 điểm) hay không
   - **Thưởng KPI**: Số tiền thưởng dựa trên KPI score

#### B. Công thức tính KPI:
- **Tỷ lệ duyệt (50 điểm tối đa)**: `(ApproveRate / 100) * 50`
- **Số lượng request đã duyệt (30 điểm tối đa)**: `(TotalApproved / 10) * 10` (mỗi 10 requests = 10 điểm)
- **Tỷ lệ từ chối thấp (20 điểm tối đa)**: `20 điểm` nếu RejectRate < 20%, ngược lại `0 điểm`
- **Tổng điểm**: Tổng 3 điểm trên
- **Đạt KPI**: >= 70 điểm

#### C. Công thức tính Bonus:
- **KPI >= 90 điểm**: 15% lương cơ bản
- **KPI >= 80 điểm**: 10% lương cơ bản
- **KPI >= 70 điểm**: 5% lương cơ bản
- **KPI < 70 điểm**: 0% (không có bonus)

### 3. Test Tích hợp vào Lương

#### A. Tạo Salary Record (nếu chưa có)
1. Admin cần tạo Salary record cho Marketing employee trong tháng cần test
2. Hoặc hệ thống sẽ tự động tạo khi tính lương

#### B. Xem Lương
1. Đăng nhập với tài khoản Marketing
2. Vào **"Lương"** trong menu
3. Chọn tháng/năm
4. Kiểm tra:
   - **Lương cơ bản**: Từ Contract hoặc Salary record
   - **Thưởng (KPI)**: Số tiền thưởng từ KPI
   - **Thực lĩnh**: Lương cơ bản + Thưởng - Phạt

#### C. Kiểm tra tự động cập nhật
- Khi xem lương, hệ thống sẽ:
  1. Kiểm tra xem có KPI chưa
  2. Nếu chưa có, tự động tính và lưu KPI
  3. Cập nhật Bonus trong Salary record nếu có KPI bonus

### 4. Test Cases

#### Test Case 1: KPI đạt (>= 70 điểm)
- **Input**: 
  - 20 requests (10 News + 10 Discount)
  - 15 approved, 2 rejected, 3 pending
  - Approve Rate: 75%
  - Reject Rate: 10%
- **Expected**:
  - Approve Rate Score: ~37.5 điểm
  - Approved Requests Score: 15 điểm (15/10 * 10, nhưng max 30)
  - Reject Rate Score: 20 điểm (vì < 20%)
  - **Total Score**: ~72.5 điểm
  - **Bonus**: 5% lương cơ bản (vì >= 70 và < 80)

#### Test Case 2: KPI không đạt (< 70 điểm)
- **Input**:
  - 10 requests
  - 5 approved, 3 rejected, 2 pending
  - Approve Rate: 50%
  - Reject Rate: 30%
- **Expected**:
  - Approve Rate Score: 25 điểm
  - Approved Requests Score: 5 điểm
  - Reject Rate Score: 0 điểm (vì >= 20%)
  - **Total Score**: 30 điểm
  - **Bonus**: 0 (không đạt KPI)

#### Test Case 3: KPI xuất sắc (>= 90 điểm)
- **Input**:
  - 30 requests
  - 28 approved, 1 rejected, 1 pending
  - Approve Rate: ~93%
  - Reject Rate: ~3%
- **Expected**:
  - Approve Rate Score: ~46.5 điểm
  - Approved Requests Score: 30 điểm (max)
  - Reject Rate Score: 20 điểm
  - **Total Score**: ~96.5 điểm
  - **Bonus**: 15% lương cơ bản

### 5. Debug nếu có vấn đề

#### Kiểm tra Database:
```sql
-- Xem KPI đã được lưu chưa
SELECT * FROM MarketingKPI WHERE EmployeeID = 'MK001' AND YEAR(KPIMonth) = 2024 AND MONTH(KPIMonth) = 12;

-- Xem Salary record
SELECT * FROM Salary WHERE EmployeeID = 'MK001' AND YEAR(SalaryMonth) = 2024 AND MONTH(SalaryMonth) = 12;

-- Xem Contract
SELECT * FROM Contract WHERE EmployeeID = 'MK001' AND Status = 'Hiệu lực';

-- Xem News và Discount requests
SELECT * FROM NewsRequest WHERE RequestedBy = 'MK001' AND YEAR(RequestedAt) = 2024 AND MONTH(RequestedAt) = 12;
SELECT * FROM DiscountRequest WHERE RequestedBy = 'MK001' AND YEAR(RequestedAt) = 2024 AND MONTH(RequestedAt) = 12;
```

#### Kiểm tra Logs:
- Mở Developer Tools (F12) → Console
- Kiểm tra có lỗi JavaScript không
- Kiểm tra Network tab xem API calls có thành công không

### 6. Lưu ý

1. **Contract phải có trước**: Hệ thống cần Contract để lấy base salary tính bonus
2. **Requests phải trong cùng tháng**: KPI chỉ tính requests trong tháng được chọn
3. **Salary record**: Cần có Salary record để xem lương, nhưng KPI có thể tính độc lập
4. **Tự động cập nhật**: Khi xem lương, hệ thống sẽ tự động tính và cập nhật KPI bonus nếu cần


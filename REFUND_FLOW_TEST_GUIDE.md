# Hướng dẫn Test Chức năng Hoàn tiền (Refund Flow)

## 📋 Tổng quan

Chức năng hoàn tiền đã được cập nhật để hỗ trợ **Test Mode** - không cần gọi MoMo API thật. Trong test mode, hệ thống sẽ simulate phản hồi từ MoMo API.

## 🔧 Cấu hình

### 1. AppSettings.json

Đã thêm 2 config mới trong `appsettings.json`:

```json
"Momo": {
  "TestMode": true,              // Bật test mode (true = test, false = production)
  "MockRefundSuccess": true      // Mock refund success (true = success, false = failure)
}
```

- **TestMode = true**: Bật test mode, không gọi MoMo API thật
- **TestMode = false**: Production mode, gọi MoMo API thật
- **MockRefundSuccess = true**: Mock response thành công
- **MockRefundSuccess = false**: Mock response thất bại (để test error handling)

## 🔄 Luồng xử lý

### 1. **Refund Request Flow**

```
1. Order có status "Chờ hoàn tiền"
   ↓
2. BManager vào trang "Danh sách Yêu cầu Hoàn tiền"
   ↓
3. BManager click "Duyệt" hoàn tiền
   ↓
4. Controller kiểm tra:
   - BranchID của user
   - Order có thuộc branch không
   - Order status = "Chờ hoàn tiền"
   ↓
5. Nếu có TransId:
   - Gọi PaymentService.RefundAsync()
   - PaymentService check TestMode:
     * TestMode = true → Return mock response
     * TestMode = false → Gọi MoMo API thật
   ↓
6. Nếu không có TransId:
   - Hoàn tiền thủ công (manual refund)
   - Update status = "Đã hoàn tiền"
   ↓
7. Cập nhật Database:
   - Status = "Đã hoàn tiền" (nếu success)
   - Status = "Hoàn tiền thất bại" (nếu failure)
   - RefundAt = DateTime.Now
   - RefundTransId = refund transaction ID
```

### 2. **Test Mode vs Production Mode**

#### **Test Mode (TestMode = true)**
- ✅ Không gọi MoMo API thật
- ✅ Return mock response ngay lập tức
- ✅ Không cần internet connection
- ✅ Không tốn phí
- ✅ Test nhanh chóng

#### **Production Mode (TestMode = false)**
- 🌐 Gọi MoMo API thật
- 🌐 Cần internet connection
- 🌐 Cần MoMo credentials hợp lệ
- 🌐 Có thể tốn phí

## 🧪 Cách Test

### 1. **Tạo Order Test với status "Chờ hoàn tiền"**

Chạy SQL script để tạo order test:

```sql
-- Tạo order test với status "Chờ hoàn tiền"
INSERT INTO [Order] (
    CustomerID, 
    BranchID, 
    OrderCode, 
    Status, 
    Total, 
    TransId, 
    CreatedAt,
    RefundAt
)
VALUES (
    1,                                    -- CustomerID (thay bằng ID thật)
    1,                                    -- BranchID (thay bằng BranchID của BManager)
    'TEST_REFUND_' + CONVERT(VARCHAR(50), NEWID()),  -- OrderCode
    'Chờ hoàn tiền',                      -- Status
    100000,                               -- Total (100,000 VNĐ)
    'TEST_TRANS_' + CONVERT(VARCHAR(50), NEWID()),  -- TransId (có thể null)
    GETDATE(),                            -- CreatedAt
    GETDATE()                             -- RefundAt (ngày yêu cầu hoàn tiền)
);
```

### 2. **Test Refund Success**

1. Đảm bảo `appsettings.json`:
   ```json
   "TestMode": true,
   "MockRefundSuccess": true
   ```

2. Login với BManager của branch có order test

3. Vào trang "Danh sách Yêu cầu Hoàn tiền"

4. Click "Duyệt" trên order test

5. Kết quả mong đợi:
   - ✅ Status order chuyển thành "Đã hoàn tiền"
   - ✅ Thông báo "Hoàn tiền thành công"
   - ✅ RefundAt được cập nhật
   - ✅ RefundTransId được lưu

### 3. **Test Refund Failure**

1. Đảm bảo `appsettings.json`:
   ```json
   "TestMode": true,
   "MockRefundSuccess": false
   ```

2. Thực hiện các bước tương tự như test success

3. Kết quả mong đợi:
   - ❌ Status order chuyển thành "Hoàn tiền thất bại"
   - ❌ Thông báo lỗi hiển thị
   - ❌ RefundAt không được cập nhật

### 4. **Test Manual Refund (không có TransId)**

1. Tạo order test không có TransId:
   ```sql
   INSERT INTO [Order] (
       CustomerID, 
       BranchID, 
       OrderCode, 
       Status, 
       Total, 
       TransId,  -- NULL
       CreatedAt,
       RefundAt
   )
   VALUES (
       1,
       1,
       'MANUAL_REFUND_' + CONVERT(VARCHAR(50), NEWID()),
       'Chờ hoàn tiền',
       100000,
       NULL,  -- Không có TransId
       GETDATE(),
       GETDATE()
   );
   ```

2. Thực hiện refund

3. Kết quả mong đợi:
   - ✅ Status order chuyển thành "Đã hoàn tiền"
   - ✅ Thông báo "Đã hoàn tiền thủ công"
   - ✅ RefundTransId = "MANUAL_..."

### 5. **Test Reject Refund**

1. Vào trang "Danh sách Yêu cầu Hoàn tiền"

2. Click "Từ chối" trên order test

3. Kết quả mong đợi:
   - ✅ Status order chuyển thành "Từ chối hoàn tiền"
   - ✅ Thông báo "Đã từ chối yêu cầu hoàn tiền"

## 📊 Console Logs

Khi test, xem console logs để debug:

### **Test Mode Success:**
```
🔧 [TEST MODE] Mock Refund Request:
   TransId: TEST_TRANS_xxx
   Amount: 100,000 VNĐ
   Description: Hoàn tiền đơn hàng TEST_REFUND_xxx từ chi nhánh 1
✅ [TEST MODE] Mock Refund Success Response
```

### **Test Mode Failure:**
```
🔧 [TEST MODE] Mock Refund Request:
   TransId: TEST_TRANS_xxx
   Amount: 100,000 VNĐ
   Description: Hoàn tiền đơn hàng TEST_REFUND_xxx từ chi nhánh 1
❌ [TEST MODE] Mock Refund Failure Response
```

### **Production Mode:**
```
🌐 [PRODUCTION MODE] Refund Request JSON: {...}
🌐 [PRODUCTION MODE] Refund Response: {...}
```

## 🚀 Chuyển sang Production

Khi sẵn sàng deploy production:

1. **Cập nhật appsettings.json:**
   ```json
   "Momo": {
     "TestMode": false,  // Tắt test mode
     "MockRefundSuccess": true  // Không cần thiết trong production
   }
   ```

2. **Đảm bảo MoMo credentials hợp lệ:**
   - PartnerCode
   - AccessKey
   - SecretKey
   - RefundEndpoint

3. **Test với MoMo API thật:**
   - Tạo order test với TransId thật
   - Thực hiện refund
   - Kiểm tra response từ MoMo

## 🐛 Troubleshooting

### **Lỗi: "Đơn hàng không tồn tại hoặc không thuộc chi nhánh của bạn"**
- ✅ Kiểm tra BranchID của order có khớp với BranchID của BManager không
- ✅ Kiểm tra OrderID có đúng không

### **Lỗi: "Đơn hàng không ở trạng thái 'Chờ hoàn tiền'"**
- ✅ Kiểm tra status của order có phải "Chờ hoàn tiền" không
- ✅ Update status: `UPDATE [Order] SET Status = 'Chờ hoàn tiền' WHERE OrderID = ?`

### **Lỗi: "Lỗi phân tích phản hồi JSON"**
- ✅ Kiểm tra TestMode có bật không
- ✅ Kiểm tra MockRefundSuccess có đúng không
- ✅ Xem console logs để debug

### **Refund không thành công trong Production Mode**
- ✅ Kiểm tra MoMo credentials
- ✅ Kiểm tra RefundEndpoint có đúng không
- ✅ Kiểm tra TransId có hợp lệ không
- ✅ Xem console logs để debug

## 📝 Notes

- **Test Mode**: Chỉ dùng để test, không gọi MoMo API thật
- **Production Mode**: Gọi MoMo API thật, cần credentials hợp lệ
- **Manual Refund**: Hoàn tiền thủ công khi không có TransId
- **Filter theo BranchID**: Mỗi BManager chỉ thấy refund requests của branch mình

## ✅ Checklist

- [ ] Test Mode hoạt động đúng
- [ ] Refund Success flow hoạt động
- [ ] Refund Failure flow hoạt động
- [ ] Manual Refund (không có TransId) hoạt động
- [ ] Reject Refund hoạt động
- [ ] Filter theo BranchID hoạt động
- [ ] Console logs hiển thị đúng
- [ ] Database được cập nhật đúng
- [ ] Thông báo success/error hiển thị đúng

---

**Tác giả**: AI Assistant  
**Ngày tạo**: 2024  
**Phiên bản**: 1.0


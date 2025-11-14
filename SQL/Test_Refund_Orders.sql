-- =============================================
-- SQL Script: Tạo Orders Test cho Refund Flow
-- =============================================
-- Mục đích: Tạo các orders test với status "Chờ hoàn tiền" để test chức năng hoàn tiền
-- Sử dụng: Chạy script này để tạo orders test trong database
-- =============================================

-- 1. Tạo Order Test với TransId (có thể refund qua MoMo)
-- =============================================
-- Lưu ý: Thay CustomerID và BranchID bằng giá trị thật trong database của bạn
DECLARE @CustomerID INT = 1;  -- Thay bằng CustomerID thật
DECLARE @BranchID INT = 1;    -- Thay bằng BranchID của BManager

-- Tạo order test với TransId
INSERT INTO [Order] (
    CustomerID, 
    BranchID, 
    OrderCode, 
    Status, 
    Total, 
    TransId, 
    CreatedAt,
    RefundAt,
    PaymentMethod,
    Address,
    ReceiverName,
    ReceiverPhone
)
VALUES (
    @CustomerID,
    @BranchID,
    'TEST_REFUND_' + CONVERT(VARCHAR(50), NEWID()),
    'Chờ hoàn tiền',
    100000,  -- 100,000 VNĐ
    'TEST_TRANS_' + CONVERT(VARCHAR(50), NEWID()),
    GETDATE(),
    GETDATE(),  -- Ngày yêu cầu hoàn tiền
    'MoMo',
    '123 Test Street',
    'Test Customer',
    '0123456789'
);

-- 2. Tạo Order Test KHÔNG có TransId (Manual Refund)
-- =============================================
INSERT INTO [Order] (
    CustomerID, 
    BranchID, 
    OrderCode, 
    Status, 
    Total, 
    TransId,  -- NULL
    CreatedAt,
    RefundAt,
    PaymentMethod,
    Address,
    ReceiverName,
    ReceiverPhone
)
VALUES (
    @CustomerID,
    @BranchID,
    'MANUAL_REFUND_' + CONVERT(VARCHAR(50), NEWID()),
    'Chờ hoàn tiền',
    50000,  -- 50,000 VNĐ
    NULL,  -- Không có TransId (Manual Refund)
    GETDATE(),
    GETDATE(),
    'Tiền mặt',
    '456 Test Street',
    'Test Customer 2',
    '0987654321'
);

-- 3. Tạo Order Test với số tiền lớn
-- =============================================
INSERT INTO [Order] (
    CustomerID, 
    BranchID, 
    OrderCode, 
    Status, 
    Total, 
    TransId, 
    CreatedAt,
    RefundAt,
    PaymentMethod,
    Address,
    ReceiverName,
    ReceiverPhone
)
VALUES (
    @CustomerID,
    @BranchID,
    'LARGE_REFUND_' + CONVERT(VARCHAR(50), NEWID()),
    'Chờ hoàn tiền',
    1000000,  -- 1,000,000 VNĐ
    'LARGE_TRANS_' + CONVERT(VARCHAR(50), NEWID()),
    GETDATE(),
    GETDATE(),
    'MoMo',
    '789 Test Street',
    'Test Customer 3',
    '0111222333'
);

-- 4. Kiểm tra Orders đã tạo
-- =============================================
SELECT 
    OrderID,
    OrderCode,
    Status,
    Total,
    TransId,
    BranchID,
    CreatedAt,
    RefundAt,
    CASE 
        WHEN TransId IS NULL THEN 'Manual Refund'
        ELSE 'MoMo Refund'
    END AS RefundType
FROM [Order]
WHERE Status = 'Chờ hoàn tiền'
    AND BranchID = @BranchID
ORDER BY RefundAt DESC;

-- 5. Xóa Orders Test (Nếu cần)
-- =============================================
-- UNCOMMENT để xóa orders test sau khi test xong
/*
DELETE FROM [OrderDetail] WHERE OrderID IN (
    SELECT OrderID FROM [Order] 
    WHERE OrderCode LIKE 'TEST_REFUND_%' 
       OR OrderCode LIKE 'MANUAL_REFUND_%'
       OR OrderCode LIKE 'LARGE_REFUND_%'
);

DELETE FROM [Order] 
WHERE OrderCode LIKE 'TEST_REFUND_%' 
   OR OrderCode LIKE 'MANUAL_REFUND_%'
   OR OrderCode LIKE 'LARGE_REFUND_%';
*/

-- =============================================
-- HƯỚNG DẪN SỬ DỤNG:
-- =============================================
-- 1. Thay @CustomerID và @BranchID bằng giá trị thật
-- 2. Chạy script để tạo orders test
-- 3. Login với BManager của BranchID đã chọn
-- 4. Vào trang "Danh sách Yêu cầu Hoàn tiền"
-- 5. Test refund flow với các orders test
-- 6. Sau khi test xong, uncomment phần 5 để xóa orders test
-- =============================================

-- 6. Tạo OrderDetail Test (Nếu cần)
-- =============================================
-- Lưu ý: Thay OrderID và ProductID bằng giá trị thật
/*
DECLARE @OrderID INT = (SELECT TOP 1 OrderID FROM [Order] WHERE OrderCode LIKE 'TEST_REFUND_%' ORDER BY OrderID DESC);
DECLARE @ProductID INT = 1;  -- Thay bằng ProductID thật
DECLARE @ProductSizeID INT = 1;  -- Thay bằng ProductSizeID thật

INSERT INTO [OrderDetail] (
    OrderID,
    ProductID,
    ProductSizeID,
    Quantity,
    UnitPrice,
    Total
)
VALUES (
    @OrderID,
    @ProductID,
    @ProductSizeID,
    2,  -- Số lượng
    50000,  -- Đơn giá
    100000  -- Tổng tiền
);
*/


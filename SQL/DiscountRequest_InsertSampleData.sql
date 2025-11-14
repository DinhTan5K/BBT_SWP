-- Script INSERT dữ liệu mẫu vào bảng DiscountRequest
-- Lưu ý: Thay đổi RequestedBy bằng EmployeeID thực tế trong database của bạn

-- Kiểm tra xem có Employee nào có RoleID = 'RM' không
-- Nếu không có, thay 'E005' bằng EmployeeID thực tế của một nhân viên

-- INSERT 1: Yêu cầu thêm mã giảm giá mới (RequestType = 0 = Add)
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    0, -- RequestType: Add (Thêm mới)
    NULL, -- DiscountId: NULL vì đây là thêm mới
    'E005', -- RequestedBy: Thay bằng EmployeeID thực tế của RM
    GETUTCDATE(), -- RequestedAt: Thời gian hiện tại
    0, -- Status: Pending (Chờ duyệt)
    NULL, -- ReviewedBy: NULL vì chưa được duyệt
    NULL, -- ReviewedAt: NULL vì chưa được duyệt
    NULL, -- RejectionReason: NULL vì chưa bị từ chối
    'SALE2024', -- Code: Mã giảm giá
    10.00, -- Percent: 10%
    NULL, -- Amount: NULL vì dùng phần trăm
    '2024-01-01 00:00:00', -- StartAt: Ngày bắt đầu
    '2024-12-31 23:59:59', -- EndAt: Ngày kết thúc
    1, -- IsActive: True (Hoạt động)
    100, -- UsageLimit: Giới hạn 100 lần sử dụng
    0 -- Type: Percentage (0 = Percentage)
);

-- INSERT 2: Yêu cầu thêm mã giảm giá với số tiền cố định
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    GETUTCDATE(),
    0, -- Pending
    'GIAM50K', -- Mã giảm 50k
    0, -- Percent: 0 vì dùng số tiền cố định
    50000, -- Amount: 50,000 VNĐ
    '2024-02-01 00:00:00',
    '2024-02-29 23:59:59',
    1,
    200,
    1 -- Type: FixedAmount (1 = FixedAmount)
);

-- INSERT 3: Yêu cầu sửa mã giảm giá (RequestType = 1 = Edit)
-- Lưu ý: Cần có DiscountId thực tế trong bảng Discount
-- Thay 1 bằng ID thực tế của Discount cần sửa
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    1, -- RequestType: Edit (Sửa)
    1, -- DiscountId: ID của Discount cần sửa (thay bằng ID thực tế)
    'E005', -- Thay bằng EmployeeID thực tế
    GETUTCDATE(),
    0, -- Pending
    'SALE2024_UPDATED', -- Code mới
    15.00, -- Percent mới: 15% (tăng từ 10%)
    NULL,
    '2024-01-01 00:00:00',
    '2024-12-31 23:59:59',
    1,
    150, -- UsageLimit mới: 150 (tăng từ 100)
    0 -- Type: Percentage
);

-- INSERT 4: Yêu cầu xóa mã giảm giá (RequestType = 2 = Delete)
-- Lưu ý: Cần có DiscountId thực tế trong bảng Discount
-- Thay 2 bằng ID thực tế của Discount cần xóa
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    2, -- RequestType: Delete (Xóa)
    2, -- DiscountId: ID của Discount cần xóa (thay bằng ID thực tế)
    'E005', -- Thay bằng EmployeeID thực tế
    GETUTCDATE(),
    0, -- Pending
    'OLD_CODE', -- Code của Discount cần xóa (để Admin biết)
    0, -- Các giá trị khác không quan trọng khi Delete
    NULL,
    NULL,
    NULL,
    1,
    NULL,
    0
);

-- INSERT 5: Yêu cầu đã được duyệt (Status = 1 = Approved)
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -1, GETUTCDATE()), -- 1 ngày trước
    1, -- Status: Approved (Đã duyệt)
    'E004', -- ReviewedBy: Admin ID (thay bằng EmployeeID thực tế của Admin)
    DATEADD(hour, -12, GETUTCDATE()), -- 12 giờ trước
    'APPROVED_CODE',
    20.00,
    NULL,
    '2024-03-01 00:00:00',
    '2024-03-31 23:59:59',
    1,
    50,
    0
);

-- INSERT 6: Yêu cầu đã bị từ chối (Status = 2 = Rejected)
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [Code],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit],
    [Type]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -2, GETUTCDATE()), -- 2 ngày trước
    2, -- Status: Rejected (Đã từ chối)
    'E004', -- ReviewedBy: Admin ID (thay bằng EmployeeID thực tế)
    DATEADD(day, -1, GETUTCDATE()), -- 1 ngày trước
    'Mã giảm giá này đã tồn tại trong hệ thống', -- Lý do từ chối
    'REJECTED_CODE',
    25.00,
    NULL,
    '2024-04-01 00:00:00',
    '2024-04-30 23:59:59',
    1,
    100,
    0
);

PRINT 'Đã insert 6 bản ghi mẫu vào bảng DiscountRequest';
PRINT 'Lưu ý: Hãy thay đổi RequestedBy và ReviewedBy bằng EmployeeID thực tế trong database của bạn';
GO

-- Query để xem dữ liệu vừa insert
SELECT 
    dr.Id,
    CASE dr.RequestType 
        WHEN 0 THEN 'Thêm mới'
        WHEN 1 THEN 'Sửa'
        WHEN 2 THEN 'Xóa'
    END AS LoaiYeuCau,
    dr.Code AS MaGiamGia,
    e1.FullName AS NguoiYeuCau,
    dr.RequestedAt AS NgayYeuCau,
    CASE dr.Status 
        WHEN 0 THEN 'Chờ duyệt'
        WHEN 1 THEN 'Đã duyệt'
        WHEN 2 THEN 'Đã từ chối'
    END AS TrangThai,
    e2.FullName AS NguoiDuyet,
    dr.ReviewedAt AS NgayDuyet,
    dr.RejectionReason AS LyDoTuChoi
FROM [dbo].[DiscountRequest] dr
LEFT JOIN [dbo].[Employee] e1 ON dr.RequestedBy = e1.EmployeeID
LEFT JOIN [dbo].[Employee] e2 ON dr.ReviewedBy = e2.EmployeeID
ORDER BY dr.RequestedAt DESC;
GO


















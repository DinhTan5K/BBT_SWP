-- Insert dữ liệu mẫu cho bảng BranchRequest
-- RequestType: 0 = Add, 1 = Edit, 2 = Delete
-- Status: 0 = Pending, 1 = Approved, 2 = Rejected
-- RequestedBy: E005 (RM - Regional Manager)
-- ReviewedBy: E004 (Admin)

-- LƯU Ý: 
-- - RegionID: 1 = Bắc, 2 = Trung, 3 = Nam
-- - Khi RequestType = Edit hoặc Delete, cần có BranchId hợp lệ trong bảng Branch (1-10)
-- - Khi RequestType = Add, BranchId = NULL
-- - Tên branch mới không được trùng với các branch hiện có

-- ============================================
-- PENDING REQUESTS (Chờ duyệt)
-- ============================================

-- 1. Request thêm chi nhánh mới - Pending
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, 
     N'Buble Tea Quận 7', 
     N'123 Đường Nguyễn Văn Linh, Phường Tân Phong, Quận 7, TP.HCM', 
     '0281234567', 
     3, -- RegionID: 3 = Nam
     N'TP.HCM', 
     10.7297, -- Latitude
     106.7028, -- Longitude
     N'Cần mở thêm chi nhánh tại Quận 7 để phục vụ khu vực dân cư mới phát triển');

-- 2. Request thêm chi nhánh mới - Pending
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, 
     N'Buble Tea Cầu Giấy', 
     N'456 Đường Trần Duy Hưng, Phường Cầu Giấy, Quận Cầu Giấy, Hà Nội', 
     '0249876543', 
     1, -- RegionID: 1 = Bắc
     N'Hà Nội', 
     21.0285, -- Latitude
     105.8542, -- Longitude
     N'Mở chi nhánh tại khu vực Cầu Giấy để mở rộng thị trường miền Bắc');

-- 3. Request sửa chi nhánh - Pending
-- Sửa BranchID = 1 (Buble Tea Royal City)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (1, 1, 'E005', GETUTCDATE(), 0, 
     N'Buble Tea Royal City Premium', 
     N'72A Nguyễn Trãi, Phường Thượng Đình, Quận Thanh Xuân, Hà Nội', 
     '0241234567', 
     1, -- RegionID: 1 = Bắc
     N'Hà Nội', 
     20.9940, 
     105.8110,
     N'Nâng cấp tên chi nhánh lên Premium và cập nhật địa chỉ');

-- 4. Request sửa chi nhánh - Pending
-- Sửa BranchID = 2 (Buble Tea Hồ Gươm)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (1, 2, 'E005', DATEADD(hour, -2, GETUTCDATE()), 0, 
     N'Buble Tea Hồ Gươm Plaza', 
     N'25 Lý Thái Tổ, Quận Hoàn Kiếm, Hà Nội', 
     '0241234568', 
     1, -- RegionID: 1 = Bắc
     N'Hà Nội', 
     21.0285, 
     105.8542,
     N'Cập nhật tên và số điện thoại mới');

-- 5. Request xóa chi nhánh - Pending
-- Xóa BranchID = 5 (Buble Tea Nguyễn Huệ) - giả sử không có nhân viên/đơn hàng
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Notes])
VALUES 
    (2, 5, 'E005', DATEADD(hour, -1, GETUTCDATE()), 0, 
     N'Buble Tea Nguyễn Huệ', 
     N'25 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh', 
     '0981111005', 
     3, -- RegionID: 3 = Nam
     N'TP.HCM',
     N'Chi nhánh này đã ngừng hoạt động, cần xóa khỏi hệ thống');

-- ============================================
-- APPROVED REQUESTS (Đã duyệt)
-- ============================================

-- 6. Request thêm chi nhánh - Approved
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', DATEADD(day, -5, GETUTCDATE()), 1, 
     'E004', DATEADD(day, -4, GETUTCDATE()), 
     N'Buble Tea Vũng Tàu', 
     N'321 Đường Trần Phú, Phường 1, TP. Vũng Tàu', 
     '0254123456', 
     3, -- RegionID: 3 = Nam
     N'Vũng Tàu', 
     10.3460, 
     107.0843,
     N'Chi nhánh mới tại Vũng Tàu phục vụ du khách');

-- 7. Request sửa chi nhánh - Approved
-- Sửa BranchID = 7 (Buble Tea Biển Nha Trang)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (1, 7, 'E005', DATEADD(day, -3, GETUTCDATE()), 1, 
     'E004', DATEADD(day, -2, GETUTCDATE()), 
     N'Buble Tea Biển Nha Trang Premium', 
     N'18 Trần Phú, TP. Nha Trang', 
     '0258123456', 
     2, -- RegionID: 2 = Trung
     N'Nha Trang', 
     12.2388, 
     109.1967,
     N'Cập nhật tên chi nhánh lên Premium');

-- 8. Request thêm chi nhánh - Approved
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', DATEADD(day, -7, GETUTCDATE()), 1, 
     'E004', DATEADD(day, -6, GETUTCDATE()), 
     N'Buble Tea Hải Phòng', 
     N'987 Đường Lạch Tray, Phường Đằng Giang, Quận Ngô Quyền, Hải Phòng', 
     '0225123456', 
     1, -- RegionID: 1 = Bắc
     N'Hải Phòng', 
     20.8449, 
     106.6881,
     N'Mở rộng thị trường miền Bắc');

-- ============================================
-- REJECTED REQUESTS (Đã từ chối)
-- ============================================

-- 9. Request thêm chi nhánh - Rejected
-- Tên trùng với branch hiện có (Buble Tea Nguyễn Huệ - BranchID 5)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], [RejectionReason], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', DATEADD(day, -10, GETUTCDATE()), 2, 
     'E004', DATEADD(day, -9, GETUTCDATE()), 
     N'Tên chi nhánh "Buble Tea Nguyễn Huệ" đã tồn tại trong region này. Vui lòng đổi tên khác.', 
     N'Buble Tea Nguyễn Huệ', 
     N'111 Đường Nguyễn Huệ, Quận 1, TP.HCM', 
     '0281111111', 
     3, -- RegionID: 3 = Nam
     N'TP.HCM', 
     10.7765, 
     106.7030,
     N'Chi nhánh tại Quận 1');

-- 10. Request sửa chi nhánh - Rejected
-- Sửa BranchID = 10 (Buble Tea Cần Thơ Riverside)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], [RejectionReason], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (1, 10, 'E005', DATEADD(day, -8, GETUTCDATE()), 2, 
     'E004', DATEADD(day, -7, GETUTCDATE()), 
     N'Thông tin địa chỉ không đầy đủ. Vui lòng bổ sung số nhà và phường/xã.', 
     N'Buble Tea Cần Thơ Riverside', 
     N'Đường 3/2', 
     '0292123456', 
     3, -- RegionID: 3 = Nam
     N'Cần Thơ', 
     10.0342, 
     105.7883,
     N'Cập nhật địa chỉ chi nhánh');

-- 11. Request xóa chi nhánh - Rejected
-- Xóa BranchID = 6 (Buble Tea Landmark 81)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [ReviewedBy], [ReviewedAt], [RejectionReason], 
     [Name], [Address], [Phone], [RegionID], [City], [Notes])
VALUES 
    (2, 6, 'E005', DATEADD(day, -6, GETUTCDATE()), 2, 
     'E004', DATEADD(day, -5, GETUTCDATE()), 
     N'Chi nhánh đang có 15 nhân viên và 120 đơn hàng liên quan. Vui lòng chuyển nhân viên và xử lý đơn hàng trước khi xóa.', 
     N'Buble Tea Landmark 81', 
     N'208 Nguyễn Hữu Cảnh, Quận Bình Thạnh, TP. Hồ Chí Minh', 
     '0981111006', 
     3, -- RegionID: 3 = Nam
     N'TP.HCM',
     N'Chi nhánh cần đóng cửa');

-- ============================================
-- THÊM MỘT SỐ REQUEST PENDING MỚI NHẤT
-- ============================================

-- 12. Request thêm chi nhánh - Pending (mới nhất)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, 
     N'Buble Tea Phú Quốc', 
     N'159 Đường Trần Hưng Đạo, Khu phố 7, Phường Dương Đông, TP. Phú Quốc, Kiên Giang', 
     '0297123456', 
     3, -- RegionID: 3 = Nam
     N'Phú Quốc', 
     10.2899, 
     103.9840,
     N'Mở chi nhánh tại Phú Quốc để phục vụ du khách quốc tế');

-- 13. Request sửa chi nhánh - Pending (mới nhất)
-- Sửa BranchID = 1 (Buble Tea Royal City)
INSERT INTO [dbo].[BranchRequest] 
    ([RequestType], [BranchId], [RequestedBy], [RequestedAt], [Status], 
     [Name], [Address], [Phone], [RegionID], [City], [Latitude], [Longitude], [Notes])
VALUES 
    (1, 1, 'E005', DATEADD(minute, -30, GETUTCDATE()), 0, 
     N'Buble Tea Royal City', 
     N'72 Nguyễn Trãi, Quận Thanh Xuân, Hà Nội', 
     '0981111001', 
     1, -- RegionID: 1 = Bắc
     N'Hà Nội', 
     20.9940, 
     105.8110,
     N'Cập nhật lại thông tin chi nhánh');

PRINT 'Đã chèn dữ liệu mẫu vào bảng BranchRequest.';
PRINT 'Tổng số: 13 bản ghi';
PRINT '  - Pending: 5 bản ghi';
PRINT '  - Approved: 3 bản ghi';
PRINT '  - Rejected: 3 bản ghi';
PRINT '';
PRINT 'LƯU Ý:';
PRINT '  - RegionID: 1 = Bắc, 2 = Trung, 3 = Nam';
PRINT '  - Cần đảm bảo RegionID (1, 2, 3) tồn tại trong bảng Region';
PRINT '  - Cần đảm bảo BranchID (1-10) tồn tại trong bảng Branch khi RequestType = Edit hoặc Delete';
PRINT '  - RequestedBy: E005 (RM)';
PRINT '  - ReviewedBy: E004 (Admin)';
PRINT '';
PRINT 'CÁC BRANCH ĐƯỢC SỬ DỤNG TRONG SAMPLE DATA:';
PRINT '  - Edit: BranchID 1, 2, 7, 10';
PRINT '  - Delete: BranchID 5, 6';
GO

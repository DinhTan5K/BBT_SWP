-- Insert dữ liệu mẫu cho bảng CategoryRequest
-- RequestType: 0 = Add, 1 = Edit, 2 = Delete
-- Status: 0 = Pending, 1 = Approved, 2 = Rejected
-- RequestedBy: E005 (RM)
-- ReviewedBy: E004 (Admin)

-- 1. Request thêm danh mục mới - Pending
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, N'Trà Sữa Đặc Biệt');

-- 2. Request thêm danh mục mới - Pending
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, N'Đồ Uống Nóng');

-- 3. Request sửa danh mục - Pending
-- Giả sử CategoryID = 1 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (1, 1, 'E005', GETUTCDATE(), 0, N'Truyền Thống (Đã cập nhật)');

-- 4. Request sửa danh mục - Pending
-- Giả sử CategoryID = 2 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (1, 2, 'E005', GETUTCDATE(), 0, N'Trái Cây Tươi');

-- 5. Request xóa danh mục - Pending
-- Giả sử CategoryID = 5 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (2, 5, 'E005', GETUTCDATE(), 0, N'Danh mục cũ');

-- 6. Request thêm danh mục - Approved
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [CategoryName])
VALUES 
    (0, NULL, 'E005', DATEADD(day, -5, GETUTCDATE()), 1, 'E004', DATEADD(day, -4, GETUTCDATE()), N'Đồ Uống Lạnh');

-- 7. Request sửa danh mục - Approved
-- Giả sử CategoryID = 3 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [CategoryName])
VALUES 
    (1, 3, 'E005', DATEADD(day, -3, GETUTCDATE()), 1, 'E004', DATEADD(day, -2, GETUTCDATE()), N'Bộ Sưu Tập Premium');

-- 8. Request thêm danh mục - Rejected
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [RejectionReason], [CategoryName])
VALUES 
    (0, NULL, 'E005', DATEADD(day, -7, GETUTCDATE()), 2, 'E004', DATEADD(day, -6, GETUTCDATE()), N'Tên danh mục không phù hợp với quy định', N'Danh mục test');

-- 9. Request sửa danh mục - Rejected
-- Giả sử CategoryID = 4 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [RejectionReason], [CategoryName])
VALUES 
    (1, 4, 'E005', DATEADD(day, -4, GETUTCDATE()), 2, 'E004', DATEADD(day, -3, GETUTCDATE()), N'Thông tin không đầy đủ, vui lòng bổ sung', N'Topping Đặc Biệt');

-- 10. Request xóa danh mục - Rejected
-- Giả sử CategoryID = 6 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [RejectionReason], [CategoryName])
VALUES 
    (2, 6, 'E005', DATEADD(day, -2, GETUTCDATE()), 2, 'E004', DATEADD(day, -1, GETUTCDATE()), N'Danh mục đang có sản phẩm, không thể xóa', N'Danh mục có sản phẩm');

-- 11. Request thêm danh mục - Pending (mới nhất)
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (0, NULL, 'E005', GETUTCDATE(), 0, N'Combo Khuyến Mãi');

-- 12. Request sửa danh mục - Pending (mới nhất)
-- Giả sử CategoryID = 1 tồn tại
INSERT INTO [dbo].[CategoryRequest] 
    ([RequestType], [CategoryId], [RequestedBy], [RequestedAt], [Status], [CategoryName])
VALUES 
    (1, 1, 'E005', GETUTCDATE(), 0, N'Truyền Thống Premium');

PRINT 'Đã chèn @ROWCOUNT dòng dữ liệu mẫu vào bảng CategoryRequest.';
GO


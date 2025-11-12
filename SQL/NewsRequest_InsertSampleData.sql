-- Insert dữ liệu mẫu cho bảng NewsRequest
-- Tạo các yêu cầu từ RM (E005) để Admin (E004) duyệt
-- Dữ liệu News và Employee đã có sẵn trong database

-- Xóa dữ liệu cũ nếu có (để test lại)
DELETE FROM [dbo].[NewsRequest];
GO

-- Lấy giá trị từ News để sử dụng
DECLARE @News1CreatedAt DATETIME2(7);
DECLARE @News2CreatedAt DATETIME2(7);
DECLARE @News3CreatedAt DATETIME2(7);
DECLARE @News4Title NVARCHAR(200);
DECLARE @News4Content NVARCHAR(MAX);
DECLARE @News4ImageUrl NVARCHAR(MAX);
DECLARE @News4CreatedAt DATETIME2(7);

SELECT @News1CreatedAt = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 1;
SELECT @News2CreatedAt = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 2;
SELECT @News3CreatedAt = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 3;
SELECT @News4Title = [Title], @News4Content = [Content], @News4ImageUrl = [ImageUrl], @News4CreatedAt = [CreatedAt] 
FROM [dbo].[News] WHERE [Id] = 4;

-- Sử dụng EmployeeID có sẵn: E004 (Admin), E005 (RM)
DECLARE @RM1 VARCHAR(10) = 'E005';
DECLARE @RM2 VARCHAR(10) = 'E005'; -- Dùng E005 cho cả 2 RM hoặc có thể tạo thêm RM khác
DECLARE @AdminID VARCHAR(10) = 'E004';

-- Insert các yêu cầu mẫu
-- 1. Yêu cầu THÊM MỚI tin tức (RequestType = 0, NewsId = NULL)
INSERT INTO [dbo].[NewsRequest] (
    [RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status],
    [Title], [Content], [ImageUrl], [CreatedAt]
)
VALUES 
    -- Yêu cầu thêm tin tức mới - Chờ duyệt
    (0, NULL, @RM1, DATEADD(DAY, -2, GETUTCDATE()), 0, -- Pending
     N'Chương trình khuyến mãi Black Friday 2025',
     N'Giảm giá lên đến 70% cho tất cả sản phẩm trà sữa trong ngày Black Friday. Áp dụng từ 00:00 ngày 29/11/2025.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     DATEADD(DAY, -2, GETUTCDATE())),
    
    -- Yêu cầu thêm tin tức mới - Đã duyệt
    (0, NULL, @RM1, DATEADD(DAY, -5, GETUTCDATE()), 1, -- Approved
     N'Ra mắt hương vị mới: Matcha Đậu Đỏ',
     N'Trải nghiệm hương vị độc đáo của Matcha Nhật Bản kết hợp với đậu đỏ truyền thống. Sản phẩm mới có mặt tại tất cả chi nhánh.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     DATEADD(DAY, -5, GETUTCDATE())),
    
    -- Yêu cầu thêm tin tức mới - Đã từ chối
    (0, NULL, @RM2, DATEADD(DAY, -3, GETUTCDATE()), 2, -- Rejected
     N'Thông báo tạm ngưng phục vụ',
     N'Do bảo trì hệ thống, tất cả chi nhánh sẽ tạm ngưng phục vụ trong ngày 30/10.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     DATEADD(DAY, -3, GETUTCDATE()));

-- Cập nhật thông tin duyệt cho các yêu cầu đã được xử lý
UPDATE [dbo].[NewsRequest]
SET [ReviewedBy] = @AdminID,
    [ReviewedAt] = DATEADD(DAY, 1, [RequestedAt]),
    [RejectionReason] = CASE 
        WHEN [Status] = 2 THEN N'Thông tin không phù hợp với chính sách công ty. Vui lòng liên hệ bộ phận Marketing để được hỗ trợ.'
        ELSE NULL
    END
WHERE [Status] IN (1, 2) AND [RequestType] = 0;
GO

-- 2. Yêu cầu SỬA tin tức (RequestType = 1, NewsId = ID của News cần sửa)
DECLARE @News1CreatedAt2 DATETIME2(7);
DECLARE @News2CreatedAt2 DATETIME2(7);
DECLARE @News3CreatedAt2 DATETIME2(7);
DECLARE @RM1_2 VARCHAR(10) = 'E005';
DECLARE @RM2_2 VARCHAR(10) = 'E005';
DECLARE @AdminID_2 VARCHAR(10) = 'E004';

SELECT @News1CreatedAt2 = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 1;
SELECT @News2CreatedAt2 = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 2;
SELECT @News3CreatedAt2 = [CreatedAt] FROM [dbo].[News] WHERE [Id] = 3;

INSERT INTO [dbo].[NewsRequest] (
    [RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status],
    [Title], [Content], [ImageUrl], [CreatedAt]
)
VALUES 
    -- Yêu cầu sửa tin tức ID 1 - Chờ duyệt
    (1, 1, @RM1_2, DATEADD(DAY, -1, GETUTCDATE()), 0, -- Pending
     N'Trà sữa mới ra mắt – Hương vị Socola Bạc Hà (CẬP NHẬT)',
     N'Thưởng thức hương vị mát lạnh của Socola hòa quyện cùng Bạc Hà. Chỉ có tại các chi nhánh BBT từ ngày 25/10! Đặc biệt: Mua 2 tặng 1 trong tuần đầu tiên.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     @News1CreatedAt2),
    
    -- Yêu cầu sửa tin tức ID 2 - Đã duyệt
    (1, 2, @RM2_2, DATEADD(DAY, -4, GETUTCDATE()), 1, -- Approved
     N'Ưu đãi 50% cho đơn hàng đầu tiên (Áp dụng đến hết tháng)',
     N'Nhập mã WELCOME50 khi đặt hàng qua website để nhận ngay ưu đãi giảm 50% cho đơn hàng đầu tiên của bạn. Áp dụng đến hết ngày 30/11/2025.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     @News2CreatedAt2),
    
    -- Yêu cầu sửa tin tức ID 3 - Đã từ chối
    (1, 3, @RM1_2, DATEADD(DAY, -2, GETUTCDATE()), 2, -- Rejected
     N'Thông báo khai trương chi nhánh mới tại Cần Thơ (SỬA LỖI)',
     N'Chúng tôi vui mừng thông báo khai trương chi nhánh mới tại trung tâm TP. Cần Thơ, phục vụ từ ngày 1/11/2025. Địa chỉ: 123 Đường ABC, Quận XYZ.',
     N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg',
     @News3CreatedAt2);

-- Cập nhật thông tin duyệt cho các yêu cầu sửa đã được xử lý
UPDATE [dbo].[NewsRequest]
SET [ReviewedBy] = @AdminID_2,
    [ReviewedAt] = DATEADD(DAY, 1, [RequestedAt]),
    [RejectionReason] = CASE 
        WHEN [Status] = 2 AND [NewsId] = 3 THEN N'Thông tin địa chỉ chưa được xác nhận. Vui lòng cung cấp địa chỉ chính xác từ bộ phận Quản lý chi nhánh.'
        ELSE NULL
    END
WHERE [RequestType] = 1 AND [Status] IN (1, 2);
GO

-- 3. Yêu cầu XÓA tin tức (RequestType = 2, NewsId = ID của News cần xóa)
DECLARE @News4Title2 NVARCHAR(200);
DECLARE @News4Content2 NVARCHAR(MAX);
DECLARE @News4ImageUrl2 NVARCHAR(MAX);
DECLARE @News4CreatedAt2 DATETIME2(7);
DECLARE @RM2_3 VARCHAR(10) = 'E005';
DECLARE @AdminID_3 VARCHAR(10) = 'E004';

SELECT @News4Title2 = [Title], @News4Content2 = [Content], @News4ImageUrl2 = [ImageUrl], @News4CreatedAt2 = [CreatedAt] 
FROM [dbo].[News] WHERE [Id] = 4;

INSERT INTO [dbo].[NewsRequest] (
    [RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status],
    [Title], [Content], [ImageUrl], [CreatedAt]
)
VALUES 
    -- Yêu cầu xóa tin tức ID 4 - Chờ duyệt
    (2, 4, @RM2_3, DATEADD(DAY, -1, GETUTCDATE()), 0, -- Pending
     @News4Title2,
     @News4Content2,
     @News4ImageUrl2,
     @News4CreatedAt2);
GO

-- Kiểm tra kết quả
SELECT 
    [Id],
    CASE [RequestType]
        WHEN 0 THEN N'Thêm mới'
        WHEN 1 THEN N'Sửa'
        WHEN 2 THEN N'Xóa'
    END AS [Loại yêu cầu],
    [NewsId],
    [RequestedBy],
    [Status],
    CASE [Status]
        WHEN 0 THEN N'Chờ duyệt'
        WHEN 1 THEN N'Đã duyệt'
        WHEN 2 THEN N'Đã từ chối'
    END AS [Trạng thái],
    [ReviewedBy],
    [Title]
FROM [dbo].[NewsRequest]
ORDER BY [RequestedAt] DESC;

-- Lấy số liệu thống kê
DECLARE @TotalCount INT;
DECLARE @PendingCount INT;
DECLARE @ApprovedCount INT;
DECLARE @RejectedCount INT;

SELECT @TotalCount = COUNT(*) FROM [dbo].[NewsRequest];
SELECT @PendingCount = COUNT(*) FROM [dbo].[NewsRequest] WHERE [Status] = 0;
SELECT @ApprovedCount = COUNT(*) FROM [dbo].[NewsRequest] WHERE [Status] = 1;
SELECT @RejectedCount = COUNT(*) FROM [dbo].[NewsRequest] WHERE [Status] = 2;

PRINT 'Đã insert dữ liệu mẫu cho NewsRequest thành công!';
PRINT 'Tổng số yêu cầu: ' + CAST(@TotalCount AS VARCHAR);
PRINT '- Chờ duyệt: ' + CAST(@PendingCount AS VARCHAR);
PRINT '- Đã duyệt: ' + CAST(@ApprovedCount AS VARCHAR);
PRINT '- Đã từ chối: ' + CAST(@RejectedCount AS VARCHAR);
GO

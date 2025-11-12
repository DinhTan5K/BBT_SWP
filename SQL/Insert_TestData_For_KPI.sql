-- ============================================
-- Script để Insert Test Data cho KPI Marketing
-- ============================================
-- LƯU Ý: Thay đổi các giá trị sau trước khi chạy:
-- 1. @MarketingEmployeeID: Mã nhân viên Marketing (VD: 'MK001')
-- 2. @AdminEmployeeID: Mã nhân viên Admin (VD: 'AD001')
-- 3. @TestMonth: Tháng test (1-12)
-- 4. @TestYear: Năm test (VD: 2024)
-- ============================================

DECLARE @MarketingEmployeeID VARCHAR(10) = 'MK001'; -- THAY ĐỔI: Mã nhân viên Marketing
DECLARE @AdminEmployeeID VARCHAR(10) = 'AD001';     -- THAY ĐỔI: Mã nhân viên Admin
DECLARE @TestMonth INT = 12;                        -- THAY ĐỔI: Tháng test (1-12)
DECLARE @TestYear INT = 2024;                      -- THAY ĐỔI: Năm test

-- Tính ngày bắt đầu và kết thúc của tháng test
DECLARE @StartDate DATETIME2 = DATEFROMPARTS(@TestYear, @TestMonth, 1);
DECLARE @EndDate DATETIME2 = EOMONTH(@StartDate);

-- RequestType: 0 = Add, 1 = Edit, 2 = Delete
-- RequestStatus: 0 = Pending, 1 = Approved, 2 = Rejected
-- DiscountType: 0 = Percent, 1 = Amount

PRINT '========================================';
PRINT 'Bắt đầu insert test data cho KPI...';
PRINT 'Marketing Employee: ' + @MarketingEmployeeID;
PRINT 'Tháng test: ' + CAST(@TestMonth AS VARCHAR(2)) + '/' + CAST(@TestYear AS VARCHAR(4));
PRINT '========================================';
PRINT '';

-- ============================================
-- 1. INSERT NEWS REQUESTS (15 requests)
-- ============================================
PRINT '1. Đang insert News Requests...';

-- 10 News Requests - APPROVED (để đạt KPI tốt)
INSERT INTO [dbo].[NewsRequest] 
    ([RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], 
     [Title], [Content], [ImageUrl], [CreatedAt], [DiscountId])
VALUES
    -- Approved News Requests
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 1, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 2, @StartDate), 
     N'Khuyến mãi Black Friday 2024', N'Nội dung chi tiết về chương trình khuyến mãi Black Friday với nhiều ưu đãi hấp dẫn...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 1, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 2, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 3, @StartDate), 
     N'Giới thiệu sản phẩm mới', N'Sản phẩm mới với thiết kế hiện đại và chất lượng cao...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 2, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 3, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 4, @StartDate), 
     N'Chương trình tri ân khách hàng', N'Cảm ơn khách hàng đã đồng hành cùng chúng tôi...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 3, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 4, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 5, @StartDate), 
     N'Khuyến mãi cuối năm', N'Chương trình khuyến mãi đặc biệt cuối năm với nhiều ưu đãi...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 4, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 5, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 6, @StartDate), 
     N'Tuyển dụng nhân viên', N'Cơ hội việc làm hấp dẫn với mức lương cạnh tranh...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 5, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 6, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 7, @StartDate), 
     N'Thông báo mở cửa hàng mới', N'Chúng tôi vui mừng thông báo khai trương cửa hàng mới...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 6, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 7, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 8, @StartDate), 
     N'Chương trình tích điểm', N'Tích điểm và đổi quà với nhiều phần quà hấp dẫn...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 7, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 8, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 9, @StartDate), 
     N'Khuyến mãi sinh nhật', N'Chương trình khuyến mãi đặc biệt nhân dịp sinh nhật...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 8, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 9, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 10, @StartDate), 
     N'Giảm giá sốc cuối tuần', N'Chương trình giảm giá sốc chỉ trong 2 ngày cuối tuần...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 9, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 10, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 11, @StartDate), 
     N'Thông báo bảo trì hệ thống', N'Hệ thống sẽ được bảo trì vào cuối tuần...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 10, @StartDate), NULL);

-- 2 News Requests - REJECTED (để test reject rate)
INSERT INTO [dbo].[NewsRequest] 
    ([RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [RejectionReason],
     [Title], [Content], [ImageUrl], [CreatedAt], [DiscountId])
VALUES
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 11, @StartDate), 2, @AdminEmployeeID, DATEADD(DAY, 12, @StartDate), 
     N'Nội dung không phù hợp với chính sách công ty', 
     N'Tin tức về sản phẩm', N'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 11, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 12, @StartDate), 2, @AdminEmployeeID, DATEADD(DAY, 13, @StartDate), 
     N'Hình ảnh không đạt chất lượng yêu cầu', 
     N'Thông báo khuyến mãi', N'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 12, @StartDate), NULL);

-- 3 News Requests - PENDING (để test pending requests)
INSERT INTO [dbo].[NewsRequest] 
    ([RequestType], [NewsId], [RequestedBy], [RequestedAt], [Status],
     [Title], [Content], [ImageUrl], [CreatedAt], [DiscountId])
VALUES
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 13, @StartDate), 0, 
     N'Tin tức chờ duyệt 1', N'Nội dung tin tức chờ duyệt...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 13, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 14, @StartDate), 0, 
     N'Tin tức chờ duyệt 2', N'Nội dung tin tức chờ duyệt...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 14, @StartDate), NULL),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 15, @StartDate), 0, 
     N'Tin tức chờ duyệt 3', N'Nội dung tin tức chờ duyệt...', 
     'https://res.cloudinary.com/do48qpmut/image/upload/v1761645429/uploads/tdppvgyas8bhvfs7lton.png', 
     DATEADD(DAY, 15, @StartDate), NULL);

PRINT '   Đã insert 15 News Requests (10 Approved, 2 Rejected, 3 Pending)';
PRINT '';

-- ============================================
-- 2. INSERT DISCOUNT REQUESTS (10 requests)
-- ============================================
PRINT '2. Đang insert Discount Requests...';

-- 7 Discount Requests - APPROVED
INSERT INTO [dbo].[DiscountRequest] 
    ([RequestType], [DiscountId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt],
     [Code], [Percent], [Amount], [StartAt], [EndAt], [IsActive], [UsageLimit], [Type])
VALUES
    -- Approved Discount Requests
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 1, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 2, @StartDate),
     'BLACKFRIDAY2024', 20.00, NULL, DATEADD(DAY, 1, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 100, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 2, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 3, @StartDate),
     'NEWYEAR2024', 15.00, NULL, DATEADD(DAY, 2, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 200, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 3, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 4, @StartDate),
     'WELCOME10', 10.00, NULL, DATEADD(DAY, 3, @StartDate), DATEADD(DAY, 60, @StartDate), 1, 500, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 4, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 5, @StartDate),
     'VIP50K', NULL, 50000.00, DATEADD(DAY, 4, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 50, 1),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 5, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 6, @StartDate),
     'SUMMER25', 25.00, NULL, DATEADD(DAY, 5, @StartDate), DATEADD(DAY, 45, @StartDate), 1, 150, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 6, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 7, @StartDate),
     'FLASH30', 30.00, NULL, DATEADD(DAY, 6, @StartDate), DATEADD(DAY, 7, @StartDate), 1, 30, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 7, @StartDate), 1, @AdminEmployeeID, DATEADD(DAY, 8, @StartDate),
     'BIRTHDAY20', 20.00, NULL, DATEADD(DAY, 7, @StartDate), DATEADD(DAY, 90, @StartDate), 1, NULL, 0);

-- 1 Discount Request - REJECTED
INSERT INTO [dbo].[DiscountRequest] 
    ([RequestType], [DiscountId], [RequestedBy], [RequestedAt], [Status], [ReviewedBy], [ReviewedAt], [RejectionReason],
     [Code], [Percent], [Amount], [StartAt], [EndAt], [IsActive], [UsageLimit], [Type])
VALUES
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 8, @StartDate), 2, @AdminEmployeeID, DATEADD(DAY, 9, @StartDate), 
     N'Mã giảm giá trùng với mã đã tồn tại',
     'DUPLICATE50', 50.00, NULL, DATEADD(DAY, 8, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 100, 0);

-- 2 Discount Requests - PENDING
INSERT INTO [dbo].[DiscountRequest] 
    ([RequestType], [DiscountId], [RequestedBy], [RequestedAt], [Status],
     [Code], [Percent], [Amount], [StartAt], [EndAt], [IsActive], [UsageLimit], [Type])
VALUES
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 9, @StartDate), 0,
     'PENDING15', 15.00, NULL, DATEADD(DAY, 9, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 100, 0),
    
    (0, NULL, @MarketingEmployeeID, DATEADD(DAY, 10, @StartDate), 0,
     'PENDING25', 25.00, NULL, DATEADD(DAY, 10, @StartDate), DATEADD(DAY, 30, @StartDate), 1, 100, 0);

PRINT '   Đã insert 10 Discount Requests (7 Approved, 1 Rejected, 2 Pending)';
PRINT '';

-- ============================================
-- TỔNG KẾT
-- ============================================
PRINT '========================================';
PRINT 'Hoàn thành insert test data!';
PRINT '';
PRINT 'Tổng kết:';
PRINT '  - News Requests: 15 (10 Approved, 2 Rejected, 3 Pending)';
PRINT '  - Discount Requests: 10 (7 Approved, 1 Rejected, 2 Pending)';
PRINT '  - Tổng Requests: 25';
PRINT '  - Tổng Approved: 17';
PRINT '  - Tổng Rejected: 3';
PRINT '  - Tổng Pending: 5';
PRINT '';
PRINT 'KPI dự kiến:';
PRINT '  - Approve Rate: ~68% (17/25)';
PRINT '  - Reject Rate: ~12% (3/25)';
PRINT '  - Approve Rate Score: ~34 điểm';
PRINT '  - Approved Requests Score: 17 điểm';
PRINT '  - Reject Rate Score: 20 điểm (vì < 20%)';
PRINT '  - Total KPI Score: ~71 điểm';
PRINT '  - KPI Status: ĐẠT (>= 70 điểm)';
PRINT '  - Bonus: 5% lương cơ bản';
PRINT '========================================';
GO


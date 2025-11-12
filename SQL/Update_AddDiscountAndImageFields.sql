-- Script tổng hợp để thêm các cột mới cho tính năng Discount và News
-- Chạy script này để cập nhật database

PRINT 'Bắt đầu cập nhật database...';
GO

-- ============================================
-- 1. Thêm DiscountId vào News và NewsRequest
-- ============================================

-- Thêm DiscountId vào bảng News
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = 'DiscountId')
BEGIN
    ALTER TABLE [dbo].[News]
    ADD [DiscountId] INT NULL;
    
    ALTER TABLE [dbo].[News]
    ADD CONSTRAINT [FK_News_DiscountId_Discount]
        FOREIGN KEY ([DiscountId])
        REFERENCES [dbo].[Discount] ([Id])
        ON DELETE NO ACTION;
    
    PRINT '✓ Đã thêm cột DiscountId vào bảng News.';
END
ELSE
BEGIN
    PRINT '⚠ Cột DiscountId đã tồn tại trong bảng News.';
END
GO

-- Thêm DiscountId vào bảng NewsRequest
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NewsRequest]') AND name = 'DiscountId')
BEGIN
    ALTER TABLE [dbo].[NewsRequest]
    ADD [DiscountId] INT NULL;
    
    ALTER TABLE [dbo].[NewsRequest]
    ADD CONSTRAINT [FK_NewsRequest_DiscountId_Discount]
        FOREIGN KEY ([DiscountId])
        REFERENCES [dbo].[Discount] ([Id])
        ON DELETE NO ACTION;
    
    PRINT '✓ Đã thêm cột DiscountId vào bảng NewsRequest.';
END
ELSE
BEGIN
    PRINT '⚠ Cột DiscountId đã tồn tại trong bảng NewsRequest.';
END
GO

-- ============================================
-- 2. Tạo Index để tối ưu query
-- ============================================

-- Index cho News.DiscountId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_News_DiscountId' AND object_id = OBJECT_ID(N'[dbo].[News]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_News_DiscountId]
        ON [dbo].[News] ([DiscountId] ASC);
    PRINT '✓ Đã tạo index IX_News_DiscountId.';
END
ELSE
BEGIN
    PRINT '⚠ Index IX_News_DiscountId đã tồn tại.';
END
GO

-- Index cho NewsRequest.DiscountId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NewsRequest_DiscountId' AND object_id = OBJECT_ID(N'[dbo].[NewsRequest]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NewsRequest_DiscountId]
        ON [dbo].[NewsRequest] ([DiscountId] ASC);
    PRINT '✓ Đã tạo index IX_NewsRequest_DiscountId.';
END
ELSE
BEGIN
    PRINT '⚠ Index IX_NewsRequest_DiscountId đã tồn tại.';
END
GO

-- ============================================
-- 3. Xóa cột ImageUrl khỏi Discount và DiscountRequest (nếu tồn tại)
-- ============================================

-- Xóa ImageUrl khỏi bảng DiscountRequest
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DiscountRequest]') AND name = 'ImageUrl')
BEGIN
    ALTER TABLE [dbo].[DiscountRequest]
    DROP COLUMN [ImageUrl];
    
    PRINT '✓ Đã xóa cột ImageUrl khỏi bảng DiscountRequest.';
END
ELSE
BEGIN
    PRINT '⚠ Cột ImageUrl không tồn tại trong bảng DiscountRequest.';
END
GO

-- Xóa ImageUrl khỏi bảng Discount
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Discount]') AND name = 'ImageUrl')
BEGIN
    ALTER TABLE [dbo].[Discount]
    DROP COLUMN [ImageUrl];
    
    PRINT '✓ Đã xóa cột ImageUrl khỏi bảng Discount.';
END
ELSE
BEGIN
    PRINT '⚠ Cột ImageUrl không tồn tại trong bảng Discount.';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'Hoàn tất cập nhật database!';
PRINT '========================================';
PRINT '';
PRINT 'Các cột đã được thêm:';
PRINT '  - News.DiscountId';
PRINT '  - NewsRequest.DiscountId';
PRINT '';
PRINT 'Các cột đã được xóa:';
PRINT '  - DiscountRequest.ImageUrl';
PRINT '  - Discount.ImageUrl';
PRINT '';
GO


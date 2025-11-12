-- Script để xóa cột ImageUrl khỏi bảng Discount và DiscountRequest
-- Vì tính năng ImageUrl cho Discount đã bị xóa

PRINT 'Bắt đầu xóa cột ImageUrl...';
GO

-- 1. Xóa cột ImageUrl khỏi bảng DiscountRequest
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

-- 2. Xóa cột ImageUrl khỏi bảng Discount
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
PRINT 'Hoàn tất xóa ImageUrl!';
PRINT '========================================';
PRINT '';
PRINT 'Các cột đã được xóa:';
PRINT '  - DiscountRequest.ImageUrl';
PRINT '  - Discount.ImageUrl';
PRINT '';
GO


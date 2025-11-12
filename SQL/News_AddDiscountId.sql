-- Thêm cột DiscountId vào bảng News và NewsRequest
-- Để liên kết News với Discount (mã giảm giá)

-- 1. Thêm DiscountId vào bảng News
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[News]') AND name = 'DiscountId')
BEGIN
    ALTER TABLE [dbo].[News]
    ADD [DiscountId] INT NULL;
    
    -- Thêm Foreign Key constraint
    ALTER TABLE [dbo].[News]
    ADD CONSTRAINT [FK_News_DiscountId_Discount]
        FOREIGN KEY ([DiscountId])
        REFERENCES [dbo].[Discount] ([Id])
        ON DELETE NO ACTION;
    
    PRINT 'Đã thêm cột DiscountId vào bảng News.';
END
ELSE
BEGIN
    PRINT 'Cột DiscountId đã tồn tại trong bảng News.';
END
GO

-- 2. Thêm DiscountId vào bảng NewsRequest
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NewsRequest]') AND name = 'DiscountId')
BEGIN
    ALTER TABLE [dbo].[NewsRequest]
    ADD [DiscountId] INT NULL;
    
    -- Thêm Foreign Key constraint
    ALTER TABLE [dbo].[NewsRequest]
    ADD CONSTRAINT [FK_NewsRequest_DiscountId_Discount]
        FOREIGN KEY ([DiscountId])
        REFERENCES [dbo].[Discount] ([Id])
        ON DELETE NO ACTION;
    
    PRINT 'Đã thêm cột DiscountId vào bảng NewsRequest.';
END
ELSE
BEGIN
    PRINT 'Cột DiscountId đã tồn tại trong bảng NewsRequest.';
END
GO

-- 3. Tạo Index để tối ưu query
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_News_DiscountId' AND object_id = OBJECT_ID(N'[dbo].[News]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_News_DiscountId]
        ON [dbo].[News] ([DiscountId] ASC);
    PRINT 'Đã tạo index IX_News_DiscountId.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NewsRequest_DiscountId' AND object_id = OBJECT_ID(N'[dbo].[NewsRequest]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NewsRequest_DiscountId]
        ON [dbo].[NewsRequest] ([DiscountId] ASC);
    PRINT 'Đã tạo index IX_NewsRequest_DiscountId.';
END
GO

PRINT 'Hoàn tất thêm DiscountId vào News và NewsRequest!';
GO


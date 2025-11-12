-- Tạo bảng ProductRequest để quản lý yêu cầu duyệt sản phẩm
-- RM (Regional Manager) tạo yêu cầu, Admin duyệt

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProductRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ProductRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Loại yêu cầu: 0 = Add (Thêm mới), 1 = Edit (Sửa), 2 = Delete (Xóa)
        [RequestType] INT NOT NULL DEFAULT 0,
        
        -- ID của Product nếu là Edit hoặc Delete (NULL nếu là Add)
        [ProductId] INT NULL,
        
        -- Thông tin người yêu cầu (RM)
        [RequestedBy] VARCHAR(10) NOT NULL,
        [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        -- Thông tin duyệt
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
        [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
        [ReviewedAt] DATETIME2(7) NULL,
        [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
        
        -- Dữ liệu Product (tất cả các trường của Product)
        [ProductName] NVARCHAR(200) NOT NULL,
        [CategoryID] INT NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Image_Url] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [ProductSizesJson] NVARCHAR(MAX) NULL, -- Lưu ProductSizes dưới dạng JSON
        
        CONSTRAINT [PK_ProductRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Keys
        CONSTRAINT [FK_ProductRequest_RequestedBy_Employee] 
            FOREIGN KEY ([RequestedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_ProductRequest_ReviewedBy_Employee] 
            FOREIGN KEY ([ReviewedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_ProductRequest_ProductId_Product] 
            FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Product] ([ProductID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_ProductRequest_CategoryID_ProductCategory] 
            FOREIGN KEY ([CategoryID]) 
            REFERENCES [dbo].[ProductCategory] ([CategoryID]) 
            ON DELETE NO ACTION
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_ProductRequest_Status] 
        ON [dbo].[ProductRequest] ([Status] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_ProductRequest_RequestedBy] 
        ON [dbo].[ProductRequest] ([RequestedBy] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_ProductRequest_RequestedAt] 
        ON [dbo].[ProductRequest] ([RequestedAt] DESC);
        
    PRINT 'Bảng ProductRequest đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng ProductRequest đã tồn tại.';
END
GO



-- Tạo bảng CategoryRequest để quản lý yêu cầu duyệt danh mục
-- RM (Regional Manager) tạo yêu cầu, Admin duyệt

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CategoryRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CategoryRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Loại yêu cầu: 0 = Add (Thêm mới), 1 = Edit (Sửa), 2 = Delete (Xóa)
        [RequestType] INT NOT NULL DEFAULT 0,
        
        -- ID của Category nếu là Edit hoặc Delete (NULL nếu là Add)
        [CategoryId] INT NULL,
        
        -- Thông tin người yêu cầu (RM)
        [RequestedBy] VARCHAR(10) NOT NULL,
        [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        -- Thông tin duyệt
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
        [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
        [ReviewedAt] DATETIME2(7) NULL,
        [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
        
        -- Dữ liệu Category
        [CategoryName] NVARCHAR(200) NOT NULL,
        
        CONSTRAINT [PK_CategoryRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Keys
        CONSTRAINT [FK_CategoryRequest_RequestedBy_Employee] 
            FOREIGN KEY ([RequestedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_CategoryRequest_ReviewedBy_Employee] 
            FOREIGN KEY ([ReviewedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_CategoryRequest_CategoryId_ProductCategory] 
            FOREIGN KEY ([CategoryId]) 
            REFERENCES [dbo].[ProductCategory] ([CategoryID]) 
            ON DELETE NO ACTION
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_CategoryRequest_Status] 
        ON [dbo].[CategoryRequest] ([Status] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_CategoryRequest_RequestedBy] 
        ON [dbo].[CategoryRequest] ([RequestedBy] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_CategoryRequest_RequestedAt] 
        ON [dbo].[CategoryRequest] ([RequestedAt] DESC);
        
    PRINT 'Bảng CategoryRequest đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng CategoryRequest đã tồn tại.';
END
GO



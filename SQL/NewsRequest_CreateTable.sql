-- Tạo bảng NewsRequest để quản lý yêu cầu duyệt tin tức
-- RM (Regional Manager) tạo yêu cầu, Admin duyệt

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NewsRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Loại yêu cầu: 0 = Add (Thêm mới), 1 = Edit (Sửa), 2 = Delete (Xóa)
        [RequestType] INT NOT NULL DEFAULT 0,
        
        -- ID của News nếu là Edit hoặc Delete (NULL nếu là Add)
        [NewsId] INT NULL,
        
        -- Thông tin người yêu cầu (RM)
        [RequestedBy] VARCHAR(10) NOT NULL,
        [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        -- Thông tin duyệt
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
        [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
        [ReviewedAt] DATETIME2(7) NULL,
        [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
        
        -- Dữ liệu News (tất cả các trường của News)
        [Title] NVARCHAR(200) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [ImageUrl] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_NewsRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Keys
        CONSTRAINT [FK_NewsRequest_RequestedBy_Employee] 
            FOREIGN KEY ([RequestedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_NewsRequest_ReviewedBy_Employee] 
            FOREIGN KEY ([ReviewedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_NewsRequest_NewsId_News] 
            FOREIGN KEY ([NewsId]) 
            REFERENCES [dbo].[News] ([Id]) 
            ON DELETE NO ACTION
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_NewsRequest_Status] 
        ON [dbo].[NewsRequest] ([Status] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_NewsRequest_RequestedBy] 
        ON [dbo].[NewsRequest] ([RequestedBy] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_NewsRequest_RequestedAt] 
        ON [dbo].[NewsRequest] ([RequestedAt] DESC);
        
    PRINT 'Bảng NewsRequest đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng NewsRequest đã tồn tại.';
END
GO


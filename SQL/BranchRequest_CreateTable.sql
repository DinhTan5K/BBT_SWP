-- Tạo bảng BranchRequest để quản lý yêu cầu duyệt chi nhánh
-- RM (Regional Manager) tạo yêu cầu, Admin duyệt

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BranchRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BranchRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Loại yêu cầu: 0 = Add (Thêm mới), 1 = Edit (Sửa), 2 = Delete (Xóa)
        [RequestType] INT NOT NULL DEFAULT 0,
        
        -- ID của Branch nếu là Edit hoặc Delete (NULL nếu là Add)
        [BranchId] INT NULL,
        
        -- Thông tin người yêu cầu (RM)
        [RequestedBy] VARCHAR(10) NOT NULL,
        [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        -- Thông tin duyệt
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
        [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
        [ReviewedAt] DATETIME2(7) NULL,
        [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
        
        -- Dữ liệu Branch
        [Name] NVARCHAR(255) NOT NULL,
        [Address] NVARCHAR(500) NULL,
        [Phone] VARCHAR(20) NULL,
        [RegionID] INT NOT NULL,
        [City] NVARCHAR(100) NULL,
        [Latitude] DECIMAL(18,15) NULL,
        [Longitude] DECIMAL(18,15) NULL,
        [Notes] NVARCHAR(1000) NULL, -- Ghi chú/Lý do
        
        CONSTRAINT [PK_BranchRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Keys
        CONSTRAINT [FK_BranchRequest_RequestedBy_Employee] 
            FOREIGN KEY ([RequestedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_BranchRequest_ReviewedBy_Employee] 
            FOREIGN KEY ([ReviewedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_BranchRequest_BranchId_Branch] 
            FOREIGN KEY ([BranchId]) 
            REFERENCES [dbo].[Branch] ([BranchID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_BranchRequest_RegionID_Region] 
            FOREIGN KEY ([RegionID]) 
            REFERENCES [dbo].[Region] ([RegionID]) 
            ON DELETE NO ACTION
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_BranchRequest_Status] 
        ON [dbo].[BranchRequest] ([Status] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_BranchRequest_RequestedBy] 
        ON [dbo].[BranchRequest] ([RequestedBy] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_BranchRequest_RequestedAt] 
        ON [dbo].[BranchRequest] ([RequestedAt] DESC);
        
    CREATE NONCLUSTERED INDEX [IX_BranchRequest_RegionID] 
        ON [dbo].[BranchRequest] ([RegionID] ASC);
    
    PRINT 'Bảng BranchRequest đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng BranchRequest đã tồn tại.';
END
GO



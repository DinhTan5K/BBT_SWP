-- Tạo bảng EmployeeBranchRequest để quản lý yêu cầu duyệt thêm/chuyển nhân viên vào chi nhánh
-- RM (Regional Manager) hoặc BM (Branch Manager) tạo yêu cầu, Admin duyệt

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeBranchRequest] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Loại yêu cầu: 0 = Add (Thêm vào chi nhánh), 1 = Edit (Chuyển chi nhánh), 2 = Delete (Xóa khỏi chi nhánh)
        [RequestType] INT NOT NULL DEFAULT 0,
        
        -- ID của Employee nếu là Edit hoặc Delete (NULL nếu là Add)
        [EmployeeId] VARCHAR(10) NULL,
        
        -- ID của Branch cần thêm/chuyển nhân viên vào
        [BranchId] INT NULL,
        
        -- Thông tin nhân viên (chỉ cần khi RequestType = Add, để tạo nhân viên mới)
        [FullName] NVARCHAR(100) NULL,
        [DateOfBirth] DATE NULL,
        [Gender] NVARCHAR(10) NULL,
        [PhoneNumber] VARCHAR(20) NULL,
        [Email] VARCHAR(100) NULL,
        [City] NVARCHAR(100) NULL,
        [Nationality] NVARCHAR(60) NULL,
        [Ethnicity] NVARCHAR(60) NULL,
        [EmergencyPhone1] VARCHAR(20) NULL,
        [EmergencyPhone2] VARCHAR(20) NULL,
        [RoleID] VARCHAR(2) NULL, -- Mặc định là "EM" khi tạo mới
        
        -- Thông tin người yêu cầu (RM hoặc BM)
        [RequestedBy] VARCHAR(10) NOT NULL,
        [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        -- Thông tin duyệt
        [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
        [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
        [ReviewedAt] DATETIME2(7) NULL,
        [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
        
        CONSTRAINT [PK_EmployeeBranchRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Keys
        CONSTRAINT [FK_EmployeeBranchRequest_RequestedBy_Employee] 
            FOREIGN KEY ([RequestedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        CONSTRAINT [FK_EmployeeBranchRequest_ReviewedBy_Employee] 
            FOREIGN KEY ([ReviewedBy]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
            
        -- Không tạo Foreign Key cho EmployeeId vì khi RequestType = Add, nhân viên có thể chưa tồn tại
        -- CONSTRAINT [FK_EmployeeBranchRequest_EmployeeId_Employee] 
        --     FOREIGN KEY ([EmployeeId]) 
        --     REFERENCES [dbo].[Employee] ([EmployeeID]) 
        --     ON DELETE NO ACTION,
            
        CONSTRAINT [FK_EmployeeBranchRequest_BranchId_Branch] 
            FOREIGN KEY ([BranchId]) 
            REFERENCES [dbo].[Branch] ([BranchID]) 
            ON DELETE NO ACTION
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_EmployeeBranchRequest_Status] 
        ON [dbo].[EmployeeBranchRequest] ([Status] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_EmployeeBranchRequest_RequestedBy] 
        ON [dbo].[EmployeeBranchRequest] ([RequestedBy] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_EmployeeBranchRequest_RequestedAt] 
        ON [dbo].[EmployeeBranchRequest] ([RequestedAt] DESC);
        
    CREATE NONCLUSTERED INDEX [IX_EmployeeBranchRequest_EmployeeId] 
        ON [dbo].[EmployeeBranchRequest] ([EmployeeId] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_EmployeeBranchRequest_BranchId] 
        ON [dbo].[EmployeeBranchRequest] ([BranchId] ASC);
        
    PRINT 'Bảng EmployeeBranchRequest đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng EmployeeBranchRequest đã tồn tại.';
END
GO


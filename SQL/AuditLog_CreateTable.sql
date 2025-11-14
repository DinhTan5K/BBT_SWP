-- Tạo bảng AuditLog để ghi lại các hành động của Admin
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLog] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AdminId] NVARCHAR(10) NOT NULL,
        [AdminName] NVARCHAR(100) NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [TargetEmployeeId] NVARCHAR(10) NULL,
        [TargetEmployeeName] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        [EntityType] NVARCHAR(50) NULL DEFAULT 'BM',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IpAddress] NVARCHAR(50) NULL,
        CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Tạo index cho AdminId để tìm kiếm nhanh
    CREATE INDEX [IX_AuditLog_AdminId] ON [dbo].[AuditLog] ([AdminId]);
    
    -- Tạo index cho TargetEmployeeId
    CREATE INDEX [IX_AuditLog_TargetEmployeeId] ON [dbo].[AuditLog] ([TargetEmployeeId]);
    
    -- Tạo index cho CreatedAt để sắp xếp theo thời gian
    CREATE INDEX [IX_AuditLog_CreatedAt] ON [dbo].[AuditLog] ([CreatedAt] DESC);
    
    -- Tạo index cho Action
    CREATE INDEX [IX_AuditLog_Action] ON [dbo].[AuditLog] ([Action]);

    PRINT 'Table AuditLog created successfully.';
END
ELSE
BEGIN
    PRINT 'Table AuditLog already exists.';
END
GO









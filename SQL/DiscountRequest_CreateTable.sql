    -- Tạo bảng DiscountRequest để quản lý yêu cầu duyệt mã giảm giá
    -- RM (Regional Manager) tạo yêu cầu, Admin duyệt

    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DiscountRequest]') AND type in (N'U'))
    BEGIN
        CREATE TABLE [dbo].[DiscountRequest] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            
            -- Loại yêu cầu: 0 = Add (Thêm mới), 1 = Edit (Sửa), 2 = Delete (Xóa)
            [RequestType] INT NOT NULL DEFAULT 0,
            
            -- ID của Discount nếu là Edit hoặc Delete (NULL nếu là Add)
            [DiscountId] INT NULL,
            
            -- Thông tin người yêu cầu (RM)
            [RequestedBy] VARCHAR(10) NOT NULL,
            [RequestedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
            
            -- Thông tin duyệt
            [Status] INT NOT NULL DEFAULT 0, -- 0 = Pending, 1 = Approved, 2 = Rejected
            [ReviewedBy] VARCHAR(10) NULL, -- Admin ID
            [ReviewedAt] DATETIME2(7) NULL,
            [RejectionReason] NVARCHAR(500) NULL, -- Lý do từ chối
            
            -- Dữ liệu Discount (tất cả các trường của Discount)
            [Code] NVARCHAR(50) NOT NULL,
            [Percent] DECIMAL(5,2) NOT NULL DEFAULT 0,
            [Amount] DECIMAL(18,2) NULL,
            [StartAt] DATETIME2(7) NULL,
            [EndAt] DATETIME2(7) NULL,
            [IsActive] BIT NOT NULL DEFAULT 1,
            [UsageLimit] INT NULL,
            [Type] INT NOT NULL DEFAULT 0, -- DiscountType enum
            
            CONSTRAINT [PK_DiscountRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
            
            -- Foreign Keys
            CONSTRAINT [FK_DiscountRequest_RequestedBy_Employee] 
                FOREIGN KEY ([RequestedBy]) 
                REFERENCES [dbo].[Employee] ([EmployeeID]) 
                ON DELETE NO ACTION,
                
            CONSTRAINT [FK_DiscountRequest_ReviewedBy_Employee] 
                FOREIGN KEY ([ReviewedBy]) 
                REFERENCES [dbo].[Employee] ([EmployeeID]) 
                ON DELETE NO ACTION,
                
            CONSTRAINT [FK_DiscountRequest_DiscountId_Discount] 
                FOREIGN KEY ([DiscountId]) 
                REFERENCES [dbo].[Discount] ([Id]) 
                ON DELETE NO ACTION
        );
        
        -- Tạo Index để tối ưu query
        CREATE NONCLUSTERED INDEX [IX_DiscountRequest_Status] 
            ON [dbo].[DiscountRequest] ([Status] ASC);
            
        CREATE NONCLUSTERED INDEX [IX_DiscountRequest_RequestedBy] 
            ON [dbo].[DiscountRequest] ([RequestedBy] ASC);
            
        CREATE NONCLUSTERED INDEX [IX_DiscountRequest_RequestedAt] 
            ON [dbo].[DiscountRequest] ([RequestedAt] DESC);
            
        PRINT 'Bảng DiscountRequest đã được tạo thành công!';
    END
    ELSE
    BEGIN
        PRINT 'Bảng DiscountRequest đã tồn tại.';
    END
    GO


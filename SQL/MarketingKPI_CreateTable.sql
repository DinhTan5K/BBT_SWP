-- Tạo bảng MarketingKPI để lưu KPI của Marketing employees
-- KPI được tính dựa trên số lượng và tỷ lệ approve của News/Discount requests

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MarketingKPI]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MarketingKPI] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        
        -- Employee ID
        [EmployeeID] VARCHAR(10) NOT NULL,
        
        -- Tháng tính KPI (luôn là ngày 01)
        [KpiMonth] DATE NOT NULL,
        
        -- Số lượng News Requests
        [TotalNewsRequests] INT NOT NULL DEFAULT 0,
        [ApprovedNewsRequests] INT NOT NULL DEFAULT 0,
        [RejectedNewsRequests] INT NOT NULL DEFAULT 0,
        [PendingNewsRequests] INT NOT NULL DEFAULT 0,
        
        -- Số lượng Discount Requests
        [TotalDiscountRequests] INT NOT NULL DEFAULT 0,
        [ApprovedDiscountRequests] INT NOT NULL DEFAULT 0,
        [RejectedDiscountRequests] INT NOT NULL DEFAULT 0,
        [PendingDiscountRequests] INT NOT NULL DEFAULT 0,
        
        -- Tỷ lệ approve (%)
        [NewsApproveRate] DECIMAL(5,2) NOT NULL DEFAULT 0,
        [DiscountApproveRate] DECIMAL(5,2) NOT NULL DEFAULT 0,
        [OverallApproveRate] DECIMAL(5,2) NOT NULL DEFAULT 0,
        
        -- Điểm KPI (0-100)
        [KPIScore] DECIMAL(5,2) NOT NULL DEFAULT 0,
        
        -- Trạng thái đạt KPI
        [IsKPIAchieved] BIT NOT NULL DEFAULT 0,
        
        -- Điểm KPI mục tiêu (mặc định 70%)
        [TargetScore] DECIMAL(5,2) NOT NULL DEFAULT 70.0,
        
        -- Bonus dựa trên KPI
        [KPIBonus] DECIMAL(18,2) NOT NULL DEFAULT 0,
        
        -- Timestamps
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2(7) NULL,
        
        CONSTRAINT [PK_MarketingKPI] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Key
        CONSTRAINT [FK_MarketingKPI_EmployeeID_Employee] 
            FOREIGN KEY ([EmployeeID]) 
            REFERENCES [dbo].[Employee] ([EmployeeID]) 
            ON DELETE NO ACTION,
        
        -- Unique constraint: Mỗi employee chỉ có 1 KPI record cho mỗi tháng
        CONSTRAINT [UQ_MarketingKPI_EmployeeID_KpiMonth] 
            UNIQUE ([EmployeeID], [KpiMonth])
    );
    
    -- Tạo Index để tối ưu query
    CREATE NONCLUSTERED INDEX [IX_MarketingKPI_EmployeeID] 
        ON [dbo].[MarketingKPI] ([EmployeeID] ASC);
        
    CREATE NONCLUSTERED INDEX [IX_MarketingKPI_KpiMonth] 
        ON [dbo].[MarketingKPI] ([KpiMonth] DESC);
        
    CREATE NONCLUSTERED INDEX [IX_MarketingKPI_KPIScore] 
        ON [dbo].[MarketingKPI] ([KPIScore] DESC);
    
    PRINT 'Bảng MarketingKPI đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng MarketingKPI đã tồn tại.';
END
GO


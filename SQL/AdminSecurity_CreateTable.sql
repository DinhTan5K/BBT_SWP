USE [start];
GO

IF OBJECT_ID('dbo.AdminSecurity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminSecurity
    (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeID        VARCHAR(10) NOT NULL,
        IsTwoFactorEnabled BIT NOT NULL DEFAULT(0),
        TwoFactorType     VARCHAR(20) NULL,          -- Email, Authenticator, SMS...
        TwoFactorSecret   NVARCHAR(256) NULL,        -- Secret key cho TOTP hoặc seed dùng mã hóa
        RecoveryCodes     NVARCHAR(MAX) NULL,        -- JSON danh sách mã backup
        LastOtpCode       NVARCHAR(64) NULL,         -- OTP gần nhất (hash hoặc đã mã hóa)
        LastOtpExpiredAt  DATETIME NULL,             -- Thời gian hết hạn OTP gần nhất
        FailedCount       INT NOT NULL DEFAULT(0),   -- Số lần nhập sai liên tiếp
        LockedUntil       DATETIME NULL,             -- Khóa tạm thời khi sai quá nhiều
        CreatedAt         DATETIME NOT NULL DEFAULT(GETUTCDATE()),
        UpdatedAt         DATETIME NOT NULL DEFAULT(GETUTCDATE())
    );

    ALTER TABLE dbo.AdminSecurity
    ADD CONSTRAINT FK_AdminSecurity_Employee
        FOREIGN KEY (EmployeeID) REFERENCES dbo.Employee(EmployeeID);

    CREATE UNIQUE INDEX IX_AdminSecurity_EmployeeID
        ON dbo.AdminSecurity(EmployeeID);
END
ELSE
BEGIN
    PRINT 'Table dbo.AdminSecurity đã tồn tại.';
END
GO



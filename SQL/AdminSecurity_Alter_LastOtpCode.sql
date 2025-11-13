USE [start];
GO

IF COL_LENGTH('dbo.AdminSecurity', 'LastOtpCode') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AdminSecurity
    ALTER COLUMN LastOtpCode NVARCHAR(64) NULL;
END
GO



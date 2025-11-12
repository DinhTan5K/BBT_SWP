-- Script để thêm các cột thông tin nhân viên vào bảng EmployeeBranchRequest
-- Các cột này chỉ cần khi RequestType = Add (để tạo nhân viên mới)

-- Kiểm tra và thêm các cột nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'FullName')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [FullName] NVARCHAR(100) NULL;
    PRINT 'Đã thêm cột FullName';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'DateOfBirth')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [DateOfBirth] DATE NULL;
    PRINT 'Đã thêm cột DateOfBirth';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [Gender] NVARCHAR(10) NULL;
    PRINT 'Đã thêm cột Gender';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'PhoneNumber')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [PhoneNumber] VARCHAR(20) NULL;
    PRINT 'Đã thêm cột PhoneNumber';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'Email')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [Email] VARCHAR(100) NULL;
    PRINT 'Đã thêm cột Email';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'City')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [City] NVARCHAR(100) NULL;
    PRINT 'Đã thêm cột City';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'Nationality')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [Nationality] NVARCHAR(60) NULL;
    PRINT 'Đã thêm cột Nationality';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'Ethnicity')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [Ethnicity] NVARCHAR(60) NULL;
    PRINT 'Đã thêm cột Ethnicity';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'EmergencyPhone1')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [EmergencyPhone1] VARCHAR(20) NULL;
    PRINT 'Đã thêm cột EmergencyPhone1';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'EmergencyPhone2')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [EmergencyPhone2] VARCHAR(20) NULL;
    PRINT 'Đã thêm cột EmergencyPhone2';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeBranchRequest]') AND name = 'RoleID')
BEGIN
    ALTER TABLE [dbo].[EmployeeBranchRequest]
    ADD [RoleID] VARCHAR(2) NULL;
    PRINT 'Đã thêm cột RoleID';
END

PRINT 'Hoàn tất thêm các cột thông tin nhân viên vào bảng EmployeeBranchRequest';
GO


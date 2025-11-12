-- Thêm cột RegionID vào bảng Employee để liên kết với Region
-- Region Manager (RM) sẽ có RegionID, các role khác có thể để NULL

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Employee]') AND name = 'RegionID')
BEGIN
    ALTER TABLE [dbo].[Employee]
    ADD [RegionID] INT NULL;

    -- Thêm Foreign Key constraint
    ALTER TABLE [dbo].[Employee]
    ADD CONSTRAINT [FK_Employee_Region_RegionID]
        FOREIGN KEY ([RegionID])
        REFERENCES [dbo].[Region] ([RegionID])
        ON DELETE NO ACTION;

    -- Tạo index cho RegionID để tăng hiệu suất truy vấn
    CREATE INDEX [IX_Employee_RegionID] ON [dbo].[Employee] ([RegionID]);

    PRINT 'Column RegionID added to Employee table successfully.';
END
ELSE
BEGIN
    PRINT 'Column RegionID already exists in Employee table.';
END
GO




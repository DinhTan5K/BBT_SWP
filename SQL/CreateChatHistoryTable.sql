-- Script tạo bảng ChatHistory để lưu lịch sử chat với AI
USE [SWP391]
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ChatHistory] (
        [ChatHistoryID] INT IDENTITY(1,1) NOT NULL,
        [CustomerID] INT NULL,
        [Question] NVARCHAR(1000) NOT NULL,
        [Answer] NTEXT NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_ChatHistory] PRIMARY KEY CLUSTERED ([ChatHistoryID] ASC),
        CONSTRAINT [FK_ChatHistory_Customer] FOREIGN KEY ([CustomerID]) 
            REFERENCES [dbo].[Customer] ([CustomerID]) ON DELETE SET NULL
    );
    
    -- Tạo index để query nhanh hơn
    CREATE INDEX [IX_ChatHistory_CustomerID_CreatedAt] ON [dbo].[ChatHistory] ([CustomerID], [CreatedAt] DESC);
    
    PRINT 'Table ChatHistory đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Table ChatHistory đã tồn tại!';
END
GO













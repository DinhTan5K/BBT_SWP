-- Create DiscountUsage table for tracking user discount usage
CREATE TABLE DiscountUsage (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DiscountId INT NOT NULL,
    UserId NVARCHAR(10) NOT NULL,
    UsedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_DiscountUsage_Discount FOREIGN KEY (DiscountId) REFERENCES Discount(Id)
);

-- Create index for faster queries on user usage
CREATE INDEX IX_DiscountUsage_UserId_DiscountId ON DiscountUsage(UserId, DiscountId);
CREATE INDEX IX_DiscountUsage_DiscountId ON DiscountUsage(DiscountId);

-- Create unique constraint to prevent duplicate usage
CREATE UNIQUE INDEX UX_DiscountUsage_UserId_DiscountId ON DiscountUsage(UserId, DiscountId);

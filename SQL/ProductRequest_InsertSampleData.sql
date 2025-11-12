-- Script INSERT dữ liệu mẫu vào bảng ProductRequest
-- Lưu ý: Thay đổi RequestedBy, CategoryID, ProductId bằng giá trị thực tế trong database của bạn

-- Kiểm tra xem có Employee nào có RoleID = 'RM' không
-- Nếu không có, thay 'E005' bằng EmployeeID thực tế của một nhân viên

-- INSERT 1: Yêu cầu thêm sản phẩm mới (RequestType = 0 = Add)
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    0, -- RequestType: Add (Thêm mới)
    NULL, -- ProductId: NULL vì đây là thêm mới
    'E005', -- RequestedBy: Thay bằng EmployeeID thực tế của RM
    GETUTCDATE(), -- RequestedAt: Thời gian hiện tại
    0, -- Status: Pending (Chờ duyệt)
    NULL, -- ReviewedBy: NULL vì chưa được duyệt
    NULL, -- ReviewedAt: NULL vì chưa được duyệt
    NULL, -- RejectionReason: NULL vì chưa bị từ chối
    N'Trà sữa Matcha Đậu Đỏ', -- ProductName: Tên sản phẩm
    1, -- CategoryID: Thay bằng CategoryID thực tế (ví dụ: 1 = Trà sữa)
    N'Trải nghiệm hương vị độc đáo của Matcha Nhật Bản kết hợp với đậu đỏ truyền thống. Sản phẩm mới có mặt tại tất cả chi nhánh.', -- Description
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url
    1, -- IsActive: True (Hoạt động)
    N'[{"Size":"S","Price":25000},{"Size":"M","Price":30000},{"Size":"L","Price":35000}]' -- ProductSizesJson: JSON chứa các size và giá
);

-- INSERT 2: Yêu cầu thêm sản phẩm mới với nhiều size
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -1, GETUTCDATE()), -- 1 ngày trước
    0, -- Pending
    N'Trà sữa Socola Bạc Hà', -- ProductName
    1, -- CategoryID: Thay bằng CategoryID thực tế
    N'Thưởng thức hương vị mát lạnh của Socola hòa quyện cùng Bạc Hà. Chỉ có tại các chi nhánh BBT từ ngày 25/10!', -- Description
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url
    1, -- IsActive
    N'[{"Size":"S","Price":28000},{"Size":"M","Price":33000},{"Size":"L","Price":38000},{"Size":"XL","Price":43000}]' -- ProductSizesJson
);

-- INSERT 3: Yêu cầu sửa sản phẩm (RequestType = 1 = Edit)
-- Lưu ý: Cần có ProductId thực tế trong bảng Product
-- Thay 1 bằng ID thực tế của Product cần sửa
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    1, -- RequestType: Edit (Sửa)
    1, -- ProductId: ID của Product cần sửa (thay bằng ID thực tế)
    'E005', -- Thay bằng EmployeeID thực tế
    GETUTCDATE(),
    0, -- Pending
    N'Trà sữa Matcha Đậu Đỏ (CẬP NHẬT)', -- ProductName mới
    1, -- CategoryID: Giữ nguyên hoặc thay đổi
    N'Trải nghiệm hương vị độc đáo của Matcha Nhật Bản kết hợp với đậu đỏ truyền thống. Sản phẩm đã được cải thiện công thức.', -- Description mới
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url mới
    1, -- IsActive
    N'[{"Size":"S","Price":27000},{"Size":"M","Price":32000},{"Size":"L","Price":37000}]' -- ProductSizesJson: Giá mới
);

-- INSERT 4: Yêu cầu xóa sản phẩm (RequestType = 2 = Delete)
-- Lưu ý: Cần có ProductId thực tế trong bảng Product
-- Thay 2 bằng ID thực tế của Product cần xóa
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    2, -- RequestType: Delete (Xóa)
    2, -- ProductId: ID của Product cần xóa (thay bằng ID thực tế)
    'E005', -- Thay bằng EmployeeID thực tế
    GETUTCDATE(),
    0, -- Pending
    N'Sản phẩm cũ cần xóa', -- ProductName của Product cần xóa (để Admin biết)
    1, -- CategoryID: Giữ nguyên
    NULL, -- Description: Không quan trọng khi Delete
    NULL, -- Image_Url: Không quan trọng khi Delete
    1, -- IsActive: Không quan trọng khi Delete
    NULL -- ProductSizesJson: Không quan trọng khi Delete
);

-- INSERT 5: Yêu cầu đã được duyệt (Status = 1 = Approved)
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -3, GETUTCDATE()), -- 3 ngày trước
    1, -- Status: Approved (Đã duyệt)
    'E004', -- ReviewedBy: Admin ID (thay bằng EmployeeID thực tế của Admin)
    DATEADD(day, -2, GETUTCDATE()), -- 2 ngày trước (đã duyệt 1 ngày sau khi yêu cầu)
    N'Trà sữa Dâu Tây', -- ProductName
    1, -- CategoryID: Thay bằng CategoryID thực tế
    N'Trà sữa với hương vị dâu tây tươi ngon, được làm từ dâu tây tự nhiên.', -- Description
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url
    1, -- IsActive
    N'[{"Size":"S","Price":26000},{"Size":"M","Price":31000},{"Size":"L","Price":36000}]' -- ProductSizesJson
);

-- INSERT 6: Yêu cầu đã bị từ chối (Status = 2 = Rejected)
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -5, GETUTCDATE()), -- 5 ngày trước
    2, -- Status: Rejected (Đã từ chối)
    'E004', -- ReviewedBy: Admin ID (thay bằng EmployeeID thực tế)
    DATEADD(day, -4, GETUTCDATE()), -- 4 ngày trước (đã từ chối 1 ngày sau khi yêu cầu)
    N'Sản phẩm này đã tồn tại trong hệ thống hoặc tên sản phẩm không phù hợp với chính sách đặt tên.', -- RejectionReason: Lý do từ chối
    N'Trà sữa Trà Xanh', -- ProductName
    1, -- CategoryID: Thay bằng CategoryID thực tế
    N'Trà sữa với hương vị trà xanh thanh mát.', -- Description
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url
    1, -- IsActive
    N'[{"Size":"S","Price":24000},{"Size":"M","Price":29000},{"Size":"L","Price":34000}]' -- ProductSizesJson
);

-- INSERT 7: Yêu cầu thêm sản phẩm không có size (ProductSizesJson = NULL)
INSERT INTO [dbo].[ProductRequest] (
    [RequestType],
    [ProductId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ProductName],
    [CategoryID],
    [Description],
    [Image_Url],
    [IsActive],
    [ProductSizesJson]
)
VALUES (
    0, -- Add
    NULL,
    'E005', -- Thay bằng EmployeeID thực tế
    DATEADD(day, -1, GETUTCDATE()),
    0, -- Pending
    N'Combo Trà sữa 2 ly', -- ProductName
    2, -- CategoryID: Thay bằng CategoryID thực tế (ví dụ: 2 = Combo)
    N'Combo đặc biệt gồm 2 ly trà sữa với giá ưu đãi.', -- Description
    N'https://res.cloudinary.com/do48qpmut/image/upload/v1761644982/uploads/uvxjrd4xtaegiquc8dy6.jpg', -- Image_Url
    1, -- IsActive
    NULL -- ProductSizesJson: NULL vì combo không có size
);

PRINT 'Đã insert 7 bản ghi mẫu vào bảng ProductRequest';
PRINT 'Lưu ý: Hãy thay đổi RequestedBy, ReviewedBy, CategoryID và ProductId bằng giá trị thực tế trong database của bạn';
GO

-- Query để xem dữ liệu vừa insert
SELECT 
    pr.Id,
    CASE pr.RequestType 
        WHEN 0 THEN N'Thêm mới'
        WHEN 1 THEN N'Sửa'
        WHEN 2 THEN N'Xóa'
    END AS LoaiYeuCau,
    pr.ProductName AS TenSanPham,
    pc.CategoryName AS DanhMuc,
    e1.FullName AS NguoiYeuCau,
    pr.RequestedAt AS NgayYeuCau,
    CASE pr.Status 
        WHEN 0 THEN N'Chờ duyệt'
        WHEN 1 THEN N'Đã duyệt'
        WHEN 2 THEN N'Đã từ chối'
    END AS TrangThai,
    e2.FullName AS NguoiDuyet,
    pr.ReviewedAt AS NgayDuyet,
    pr.RejectionReason AS LyDoTuChoi,
    pr.ProductSizesJson AS KichThuocVaGia
FROM [dbo].[ProductRequest] pr
LEFT JOIN [dbo].[Employee] e1 ON pr.RequestedBy = e1.EmployeeID
LEFT JOIN [dbo].[Employee] e2 ON pr.ReviewedBy = e2.EmployeeID
LEFT JOIN [dbo].[ProductCategory] pc ON pr.CategoryID = pc.CategoryID
ORDER BY pr.RequestedAt DESC;
GO

-- Lấy số liệu thống kê
DECLARE @TotalCount INT;
DECLARE @PendingCount INT;
DECLARE @ApprovedCount INT;
DECLARE @RejectedCount INT;

SELECT @TotalCount = COUNT(*) FROM [dbo].[ProductRequest];
SELECT @PendingCount = COUNT(*) FROM [dbo].[ProductRequest] WHERE [Status] = 0;
SELECT @ApprovedCount = COUNT(*) FROM [dbo].[ProductRequest] WHERE [Status] = 1;
SELECT @RejectedCount = COUNT(*) FROM [dbo].[ProductRequest] WHERE [Status] = 2;

PRINT 'Tổng số yêu cầu: ' + CAST(@TotalCount AS VARCHAR);
PRINT '- Chờ duyệt: ' + CAST(@PendingCount AS VARCHAR);
PRINT '- Đã duyệt: ' + CAST(@ApprovedCount AS VARCHAR);
PRINT '- Đã từ chối: ' + CAST(@RejectedCount AS VARCHAR);
GO



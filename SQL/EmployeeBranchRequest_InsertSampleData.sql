 -- Script INSERT dữ liệu mẫu vào bảng EmployeeBranchRequest
-- QUY TẮC:
-- 1. BM (Branch Manager) chỉ có thể thêm nhân viên vào chi nhánh của mình
-- 2. BM chỉ có thể edit (chuyển) nhân viên EM (Employee) đang công tác tại chi nhánh của mình
-- E001 (Nguyễn Hoàng Nam) là BM của chi nhánh 1 (Buble Tea Royal City)
-- E002 (Lê Thanh Hòa) là SL ở chi nhánh 1
-- E003 (Phạm Minh Khang) là EM ở chi nhánh 1

-- LƯU Ý: 
-- - E003 đã có BranchID = 1 rồi, nên không thêm vào chi nhánh 1 nữa
-- - Cần tạo các nhân viên mới (E006, E007, E008, E009) với BranchID = NULL trước
-- - Hoặc chuyển nhân viên EM đang ở chi nhánh 1 sang chi nhánh khác

-- ============================================
-- LƯU Ý: 
-- - Khi RequestType = Add, EmployeeId có thể là NULL hoặc chưa tồn tại trong bảng Employee
-- - Khi RequestType = Edit/Delete, EmployeeId phải tồn tại và đang ở chi nhánh của BM
-- - Foreign Key cho EmployeeId đã được bỏ để cho phép tạo yêu cầu cho nhân viên chưa tồn tại
-- ============================================

-- QUAN TRỌNG: Nếu bảng đã được tạo với Foreign Key constraint, cần chạy script sau trước:
-- SQL/EmployeeBranchRequest_AlterTable_RemoveFK.sql
-- để xóa Foreign Key constraint cho EmployeeId

-- Xóa dữ liệu cũ nếu có (để test lại)
-- DELETE FROM [dbo].[EmployeeBranchRequest];
-- GO

-- ============================================
-- INSERT 1: Yêu cầu thêm nhân viên EM mới vào chi nhánh 1 (RequestType = 0 = Add)
-- BM (E001) yêu cầu thêm nhân viên EM mới vào chi nhánh 1
-- Lưu ý: EmployeeId = NULL, điền đầy đủ thông tin nhân viên để tạo mới khi duyệt
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [FullName],
    [DateOfBirth],
    [Gender],
    [PhoneNumber],
    [Email],
    [City],
    [Nationality],
    [Ethnicity],
    [EmergencyPhone1],
    [EmergencyPhone2],
    [RoleID]
)
VALUES (
    0, -- RequestType: Add (Thêm vào chi nhánh)
    NULL, -- EmployeeId: NULL vì đây là nhân viên mới chưa có trong hệ thống
    1, -- BranchId: Buble Tea Royal City (chi nhánh của BM E001)
    'E001', -- RequestedBy: Nguyễn Hoàng Nam (BM của chi nhánh 1)
    DATEADD(day, -3, GETUTCDATE()), -- RequestedAt: 3 ngày trước
    0, -- Status: Pending (Chờ duyệt)
    NULL, -- ReviewedBy: NULL vì chưa được duyệt
    NULL, -- ReviewedAt: NULL vì chưa được duyệt
    NULL, -- RejectionReason: NULL vì chưa bị từ chối
    N'Trần Văn Đức', -- FullName
    '2000-05-15', -- DateOfBirth
    N'Nam', -- Gender
    '0911111111', -- PhoneNumber
    'duc.tran@btea.com', -- Email
    N'Hà Nội', -- City
    N'Việt Nam', -- Nationality
    N'Kinh', -- Ethnicity
    '0911111112', -- EmergencyPhone1
    '0911111113', -- EmergencyPhone2
    'EM' -- RoleID
);

-- ============================================
-- INSERT 2: Yêu cầu chuyển nhân viên EM từ chi nhánh 1 sang chi nhánh khác (RequestType = 1 = Edit)
-- BM (E001) yêu cầu chuyển nhân viên EM (E003) từ chi nhánh 1 sang chi nhánh 2
-- Lưu ý: Chỉ có thể chuyển nhân viên EM đang ở chi nhánh 1
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Edit
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName],
    [DateOfBirth],
    [Gender],
    [PhoneNumber],
    [Email],
    [City],
    [Nationality],
    [Ethnicity],
    [EmergencyPhone1],
    [EmergencyPhone2],
    [RoleID]
)
VALUES (
    1, -- RequestType: Edit (Chuyển chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    2, -- BranchId: Buble Tea Hồ Gươm (chi nhánh đích)
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(day, -2, GETUTCDATE()), -- 2 ngày trước
    0, -- Pending
    NULL, -- FullName: NULL vì không cần cho Edit
    NULL, -- DateOfBirth: NULL
    NULL, -- Gender: NULL
    NULL, -- PhoneNumber: NULL
    NULL, -- Email: NULL
    NULL, -- City: NULL
    NULL, -- Nationality: NULL
    NULL, -- Ethnicity: NULL
    NULL, -- EmergencyPhone1: NULL
    NULL, -- EmergencyPhone2: NULL
    NULL  -- RoleID: NULL
);

-- ============================================
-- INSERT 3: Yêu cầu đã được duyệt - Thêm EM mới vào chi nhánh 1 (Status = 1 = Approved)
-- BM (E001) yêu cầu thêm nhân viên EM mới vào chi nhánh 1 - Đã được Admin duyệt
-- Lưu ý: Sau khi duyệt, nhân viên đã được tạo với EmployeeID = 'E007'
-- ============================================
-- ============================================
-- INSERT 4: Yêu cầu đã bị từ chối - Thêm EM mới vào chi nhánh 1 (Status = 2 = Rejected)
-- BM (E001) yêu cầu thêm nhân viên EM mới vào chi nhánh 1 - Bị từ chối
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [RejectionReason],
    [FullName],
    [DateOfBirth],
    [Gender],
    [PhoneNumber],
    [Email],
    [City],
    [Nationality],
    [Ethnicity],
    [EmergencyPhone1],
    [EmergencyPhone2],
    [RoleID]
)
VALUES (
    0, -- Add
    NULL, -- EmployeeId: NULL vì yêu cầu bị từ chối, nhân viên chưa được tạo
    1, -- BranchId: Buble Tea Royal City (chi nhánh của BM)
    'E001', -- RequestedBy: BM
    DATEADD(day, -7, GETUTCDATE()), -- 7 ngày trước
    2, -- Status: Rejected (Đã từ chối)
    'E004', -- ReviewedBy: Admin
    DATEADD(day, -6, GETUTCDATE()), -- 6 ngày trước
    N'Nhân viên này không đủ điều kiện để làm việc tại chi nhánh này', -- Lý do từ chối
    N'Lê Văn Hùng', -- FullName
    '2001-03-10', -- DateOfBirth
    N'Nam', -- Gender
    '0933333333', -- PhoneNumber
    'hung.le@btea.com', -- Email
    N'TP. Hồ Chí Minh', -- City
    N'Việt Nam', -- Nationality
    N'Kinh', -- Ethnicity
    '0933333334', -- EmergencyPhone1
    '0933333335', -- EmergencyPhone2
    'EM' -- RoleID
);

-- ============================================
-- INSERT 5: Yêu cầu chuyển EM từ chi nhánh 1 sang chi nhánh 3 - Chờ duyệt
-- BM (E001) yêu cầu chuyển nhân viên EM (E003) từ chi nhánh 1 sang chi nhánh 3
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Edit
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName], [DateOfBirth], [Gender], [PhoneNumber], [Email], [City],
    [Nationality], [Ethnicity], [EmergencyPhone1], [EmergencyPhone2], [RoleID]
)
VALUES (
    1, -- Edit (Chuyển chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    3, -- BranchId: Buble Tea Đà Nẵng Riverside (chi nhánh đích)
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(hour, -12, GETUTCDATE()), -- 12 giờ trước
    0, -- Pending
    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
);

-- ============================================
-- INSERT 6: Yêu cầu chuyển EM từ chi nhánh 1 sang chi nhánh 5 - Chờ duyệt
-- BM (E001) yêu cầu chuyển nhân viên EM (E003) từ chi nhánh 1 sang chi nhánh 5
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Edit
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName], [DateOfBirth], [Gender], [PhoneNumber], [Email], [City],
    [Nationality], [Ethnicity], [EmergencyPhone1], [EmergencyPhone2], [RoleID]
)
VALUES (
    1, -- Edit (Chuyển chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    5, -- BranchId: Buble Tea Nguyễn Huệ (chi nhánh đích)
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(hour, -6, GETUTCDATE()), -- 6 giờ trước
    0, -- Pending
    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
);

-- ============================================
-- INSERT 7: Yêu cầu chuyển EM từ chi nhánh 1 sang chi nhánh 7 - Đã duyệt
-- BM (E001) yêu cầu chuyển nhân viên EM (E003) từ chi nhánh 1 sang chi nhánh 7 - Đã được Admin duyệt
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Edit
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [ReviewedBy],
    [ReviewedAt],
    [FullName], [DateOfBirth], [Gender], [PhoneNumber], [Email], [City],
    [Nationality], [Ethnicity], [EmergencyPhone1], [EmergencyPhone2], [RoleID]
)
VALUES (
    1, -- Edit (Chuyển chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    7, -- BranchId: Buble Tea Biển Nha Trang (chi nhánh đích)
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(day, -10, GETUTCDATE()), -- 10 ngày trước
    1, -- Approved
    'E004', -- ReviewedBy: Admin
    DATEADD(day, -9, GETUTCDATE()), -- 9 ngày trước
    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
);

-- ============================================
-- INSERT 8: Yêu cầu xóa EM khỏi chi nhánh 1 (RequestType = 2 = Delete)
-- BM (E001) yêu cầu xóa nhân viên EM (E003) khỏi chi nhánh 1
-- Lưu ý: Chỉ có thể xóa EM đang ở chi nhánh của BM
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Delete
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName], [DateOfBirth], [Gender], [PhoneNumber], [Email], [City],
    [Nationality], [Ethnicity], [EmergencyPhone1], [EmergencyPhone2], [RoleID]
)
VALUES (
    2, -- RequestType: Delete (Xóa khỏi chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    NULL, -- BranchId: NULL vì là xóa khỏi chi nhánh
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(hour, -3, GETUTCDATE()), -- 3 giờ trước
    0, -- Pending
    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
);

-- ============================================
-- INSERT 9: Yêu cầu thêm EM mới vào chi nhánh 1 - Chờ duyệt (mới nhất)
-- BM (E001) yêu cầu thêm nhân viên EM mới vào chi nhánh 1
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName],
    [DateOfBirth],
    [Gender],
    [PhoneNumber],
    [Email],
    [City],
    [Nationality],
    [Ethnicity],
    [EmergencyPhone1],
    [EmergencyPhone2],
    [RoleID]
)
VALUES (
    0, -- Add
    NULL, -- EmployeeId: NULL vì đây là nhân viên mới chưa có trong hệ thống
    1, -- BranchId: Buble Tea Royal City (chi nhánh của BM)
    'E001', -- RequestedBy: BM
    GETUTCDATE(), -- Vừa mới tạo
    0, -- Pending
    N'Phạm Thị Lan', -- FullName
    '2000-11-25', -- DateOfBirth
    N'Nữ', -- Gender
    '0944444444', -- PhoneNumber
    'lan.pham@btea.com', -- Email
    N'Cần Thơ', -- City
    N'Việt Nam', -- Nationality
    N'Kinh', -- Ethnicity
    '0944444445', -- EmergencyPhone1
    '0944444446', -- EmergencyPhone2
    'EM' -- RoleID
);

-- ============================================
-- INSERT 10: Yêu cầu chuyển EM từ chi nhánh 1 sang chi nhánh 4 - Chờ duyệt
-- BM (E001) yêu cầu chuyển nhân viên EM (E003) từ chi nhánh 1 sang chi nhánh 4
-- Các cột thông tin nhân viên để NULL vì không cần thiết cho Edit
-- ============================================
INSERT INTO [dbo].[EmployeeBranchRequest] (
    [RequestType],
    [EmployeeId],
    [BranchId],
    [RequestedBy],
    [RequestedAt],
    [Status],
    [FullName], [DateOfBirth], [Gender], [PhoneNumber], [Email], [City],
    [Nationality], [Ethnicity], [EmergencyPhone1], [EmergencyPhone2], [RoleID]
)
VALUES (
    1, -- Edit (Chuyển chi nhánh)
    'E003', -- EmployeeId: Phạm Minh Khang (EM) - đang ở chi nhánh 1
    4, -- BranchId: Buble Tea Cầu Rồng (chi nhánh đích)
    'E001', -- RequestedBy: BM của chi nhánh 1
    DATEADD(hour, -1, GETUTCDATE()), -- 1 giờ trước
    0, -- Pending
    NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
);

PRINT 'Đã insert 10 bản ghi mẫu vào bảng EmployeeBranchRequest';
PRINT 'Tất cả yêu cầu đều được tạo bởi BM (E001 - Nguyễn Hoàng Nam) của chi nhánh 1';
PRINT '';
PRINT 'QUY TẮC:';
PRINT '  - BM chỉ có thể thêm nhân viên MỚI (chưa có BranchID) vào chi nhánh của mình (chi nhánh 1)';
PRINT '  - BM chỉ có thể edit (chuyển) nhân viên EM đang công tác tại chi nhánh của mình';
PRINT '';
PRINT 'LƯU Ý:';
PRINT '  - E003 đã có BranchID = 1 rồi, nên không tạo yêu cầu thêm vào chi nhánh 1 nữa';
PRINT '  - Khi RequestType = Add, EmployeeId có thể là NULL (nhân viên mới chưa có trong hệ thống)';
PRINT '  - Khi RequestType = Edit/Delete, EmployeeId phải tồn tại (như E003)';
PRINT '  - Chỉ tạo yêu cầu chuyển cho E003 (EM đang ở chi nhánh 1)';
GO

-- ============================================
-- Query để xem dữ liệu vừa insert
-- ============================================
SELECT 
    ebr.Id,
    CASE ebr.RequestType 
        WHEN 0 THEN 'Thêm vào chi nhánh'
        WHEN 1 THEN 'Chuyển chi nhánh'
        WHEN 2 THEN 'Xóa khỏi chi nhánh'
    END AS LoaiYeuCau,
    e.FullName AS TenNhanVien,
    e.EmployeeID AS MaNhanVien,
    b.Name AS TenChiNhanh,
    ebr.BranchId AS MaChiNhanh,
    e1.FullName AS NguoiYeuCau,
    ebr.RequestedAt AS NgayYeuCau,
    CASE ebr.Status 
        WHEN 0 THEN 'Chờ duyệt'
        WHEN 1 THEN 'Đã duyệt'
        WHEN 2 THEN 'Đã từ chối'
    END AS TrangThai,
    e2.FullName AS NguoiDuyet,
    ebr.ReviewedAt AS NgayDuyet,
    ebr.RejectionReason AS LyDoTuChoi
FROM [dbo].[EmployeeBranchRequest] ebr
LEFT JOIN [dbo].[Employee] e ON ebr.EmployeeId = e.EmployeeID
LEFT JOIN [dbo].[Branch] b ON ebr.BranchId = b.BranchID
LEFT JOIN [dbo].[Employee] e1 ON ebr.RequestedBy = e1.EmployeeID
LEFT JOIN [dbo].[Employee] e2 ON ebr.ReviewedBy = e2.EmployeeID
ORDER BY ebr.RequestedAt DESC;
GO


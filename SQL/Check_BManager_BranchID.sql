-- =============================================
-- SQL Script: Kiểm tra và cập nhật BranchID cho BManager
-- =============================================
-- Mục đích: Kiểm tra xem BManager có BranchID trong database không
-- Sử dụng: Chạy script này để kiểm tra và cập nhật BranchID cho BManager
-- =============================================

-- 1. Kiểm tra tất cả BManager (BM) và BranchID của họ
-- =============================================
SELECT 
    e.EmployeeID,
    e.FullName,
    e.Email,
    e.RoleID,
    e.BranchID,
    b.Name AS BranchName,
    CASE 
        WHEN e.BranchID IS NULL THEN '❌ CHƯA CÓ BRANCHID'
        WHEN b.BranchID IS NULL THEN '❌ BRANCHID KHÔNG TỒN TẠI'
        ELSE '✅ OK'
    END AS Status
FROM [Employee] e
LEFT JOIN [Branch] b ON e.BranchID = b.BranchID
WHERE e.RoleID = 'BM'  -- Branch Manager
ORDER BY e.BranchID, e.FullName;

-- 2. Kiểm tra các BManager chưa có BranchID
-- =============================================
SELECT 
    e.EmployeeID,
    e.FullName,
    e.Email,
    e.RoleID,
    e.BranchID
FROM [Employee] e
WHERE e.RoleID = 'BM' 
    AND e.BranchID IS NULL;

-- 3. Kiểm tra các BManager có BranchID nhưng Branch không tồn tại
-- =============================================
SELECT 
    e.EmployeeID,
    e.FullName,
    e.Email,
    e.RoleID,
    e.BranchID
FROM [Employee] e
WHERE e.RoleID = 'BM' 
    AND e.BranchID IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM [Branch] b WHERE b.BranchID = e.BranchID
    );

-- 4. Liệt kê tất cả Branch có sẵn để gán cho BManager
-- =============================================
SELECT 
    b.BranchID,
    b.Name AS BranchName,
    b.Address,
    b.Phone,
    b.RegionID,
    r.RegionName,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM [Employee] e 
            WHERE e.BranchID = b.BranchID 
                AND e.RoleID = 'BM' 
                AND e.IsActive = 1
        ) THEN '✅ Đã có BManager'
        ELSE '❌ Chưa có BManager'
    END AS HasManager
FROM [Branch] b
LEFT JOIN [Region] r ON b.RegionID = r.RegionID
WHERE b.IsActive = 1
ORDER BY b.BranchID;

-- 5. Cập nhật BranchID cho BManager (UNCOMMENT và chạy nếu cần)
-- =============================================
-- Lưu ý: Thay @EmployeeID và @BranchID bằng giá trị thật
/*
DECLARE @EmployeeID VARCHAR(10) = 'BM001';  -- Thay bằng EmployeeID của BManager
DECLARE @BranchID INT = 1;  -- Thay bằng BranchID muốn gán

-- Kiểm tra Employee có tồn tại không
IF EXISTS (SELECT 1 FROM [Employee] WHERE EmployeeID = @EmployeeID AND RoleID = 'BM')
BEGIN
    -- Kiểm tra Branch có tồn tại không
    IF EXISTS (SELECT 1 FROM [Branch] WHERE BranchID = @BranchID)
    BEGIN
        -- Cập nhật BranchID
        UPDATE [Employee]
        SET BranchID = @BranchID
        WHERE EmployeeID = @EmployeeID;
        
        PRINT '✅ Đã cập nhật BranchID cho BManager: ' + @EmployeeID + ' -> BranchID: ' + CAST(@BranchID AS VARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT '❌ BranchID không tồn tại: ' + CAST(@BranchID AS VARCHAR(10));
    END
END
ELSE
BEGIN
    PRINT '❌ Employee không tồn tại hoặc không phải BManager: ' + @EmployeeID;
END
*/

-- 6. Gán BranchID cho tất cả BManager chưa có BranchID (dựa trên Region)
-- =============================================
-- Lưu ý: Script này sẽ gán BranchID đầu tiên của Region cho BManager nếu BManager có RegionID
-- UNCOMMENT và chạy nếu cần
/*
UPDATE e
SET e.BranchID = (
    SELECT TOP 1 b.BranchID 
    FROM [Branch] b 
    WHERE b.RegionID = e.RegionID 
        AND b.IsActive = 1
    ORDER BY b.BranchID
)
FROM [Employee] e
WHERE e.RoleID = 'BM'
    AND e.BranchID IS NULL
    AND e.RegionID IS NOT NULL
    AND EXISTS (
        SELECT 1 FROM [Branch] b 
        WHERE b.RegionID = e.RegionID 
            AND b.IsActive = 1
    );

PRINT '✅ Đã cập nhật BranchID cho các BManager có RegionID';
*/

-- 7. Kiểm tra lại sau khi cập nhật
-- =============================================
SELECT 
    e.EmployeeID,
    e.FullName,
    e.Email,
    e.RoleID,
    e.BranchID,
    b.Name AS BranchName,
    CASE 
        WHEN e.BranchID IS NULL THEN '❌ CHƯA CÓ BRANCHID'
        WHEN b.BranchID IS NULL THEN '❌ BRANCHID KHÔNG TỒN TẠI'
        ELSE '✅ OK'
    END AS Status
FROM [Employee] e
LEFT JOIN [Branch] b ON e.BranchID = b.BranchID
WHERE e.RoleID = 'BM'
ORDER BY e.BranchID, e.FullName;

-- =============================================
-- HƯỚNG DẪN SỬ DỤNG:
-- =============================================
-- 1. Chạy script 1 để kiểm tra tất cả BManager và BranchID
-- 2. Nếu có BManager chưa có BranchID, chạy script 4 để xem các Branch có sẵn
-- 3. Sử dụng script 5 để cập nhật BranchID cho BManager cụ thể
-- 4. Hoặc sử dụng script 6 để tự động gán BranchID dựa trên RegionID
-- 5. Chạy script 7 để kiểm tra lại sau khi cập nhật
-- =============================================


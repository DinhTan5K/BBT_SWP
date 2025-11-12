-- Script để kiểm tra kiểu dữ liệu của EmployeeID trong bảng Employee
-- Chạy script này trước để xác định kiểu dữ liệu chính xác

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Employee' 
    AND COLUMN_NAME = 'EmployeeID';

GO













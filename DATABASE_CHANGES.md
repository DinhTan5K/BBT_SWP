# Thay đổi Database cho tính năng Check-in/Check-out

## 1. Bảng mới: **Attendance**

Tạo bảng mới để lưu thông tin check-in/check-out:

```sql
CREATE TABLE [Attendance] (
    [AttendanceID] int IDENTITY(1,1) PRIMARY KEY,
    [EmployeeID] nvarchar(10) NOT NULL,
    [WorkScheduleID] int NULL,
    [CheckInTime] datetime2 NOT NULL,
    [CheckOutTime] datetime2 NULL,
    [CheckInImageUrl] nvarchar(500) NULL,
    [CheckOutImageUrl] nvarchar(500) NULL,
    [CheckInLocation] nvarchar(200) NULL,
    [CheckOutLocation] nvarchar(200) NULL,
    [IsFaceVerified] bit NOT NULL DEFAULT 0,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    
    FOREIGN KEY ([EmployeeID]) REFERENCES [Employee]([EmployeeID]),
    FOREIGN KEY ([WorkScheduleID]) REFERENCES [WorkSchedule]([WorkScheduleID])
);
```

**Các trường quan trọng:**
- `AttendanceID` - ID tự tăng (Primary Key)
- `EmployeeID` - Mã nhân viên (Foreign Key → Employee)
- `WorkScheduleID` - Mã ca làm việc (Foreign Key → WorkSchedule, nullable)
- `CheckInTime` - Thời gian check-in (NOT NULL)
- `CheckOutTime` - Thời gian check-out (NULL - chưa check-out)
- `CheckInImageUrl` - Đường dẫn ảnh check-in
- `CheckOutImageUrl` - Đường dẫn ảnh check-out
- `IsFaceVerified` - Đã xác thực khuôn mặt (mặc định: false)

## 2. Bảng Employee: Thêm cột mới

Thêm cột `FaceImageUrl` vào bảng `Employee`:

```sql
ALTER TABLE [Employee]
ADD [FaceImageUrl] nvarchar(300) NULL;
```

**Mục đích:**
- Lưu đường dẫn ảnh khuôn mặt đã đăng ký của nhân viên
- Dùng để so sánh khi check-in/check-out
- NULL nếu nhân viên chưa upload ảnh khuôn mặt

## 3. Quan hệ (Foreign Keys)

- `Attendance.EmployeeID` → `Employee.EmployeeID`
- `Attendance.WorkScheduleID` → `WorkSchedule.WorkScheduleID`

---

## Cách thực hiện Migration

### Option 1: Dùng Entity Framework Migration (Khuyên dùng)

```bash
# Tạo migration
dotnet ef migrations add AddAttendanceAndFaceImage

# Xem migration file được tạo (tùy chọn)
# File sẽ ở: Migrations/[timestamp]_AddAttendanceAndFaceImage.cs

# Cập nhật database
dotnet ef database update
```

### Option 2: Chạy SQL trực tiếp (Nếu không dùng EF Migration)

Chạy các lệnh SQL trên trong SQL Server Management Studio hoặc công cụ quản lý database.

---

## Kiểm tra sau khi migration

1. **Kiểm tra bảng Attendance đã được tạo:**
```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Attendance';
```

2. **Kiểm tra cột FaceImageUrl đã được thêm vào Employee:**
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FaceImageUrl';
```

3. **Kiểm tra Foreign Keys:**
```sql
SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name = 'Attendance';
```

---

## Lưu ý

1. **Backup database trước khi migration** (nếu có dữ liệu quan trọng)
2. **FaceImageUrl là nullable** - nhân viên có thể chưa upload ảnh ngay
3. **CheckOutTime là nullable** - cho phép nhân viên chỉ check-in mà chưa check-out
4. **WorkScheduleID là nullable** - cho phép check-in không liên kết với ca làm việc cụ thể





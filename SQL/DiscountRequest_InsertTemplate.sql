-- Template INSERT đơn giản cho DiscountRequest
-- Chỉ cần điền các giá trị cần thiết

-- ============================================
-- INSERT YÊU CẦU THÊM MÃ GIẢM GIÁ MỚI (Add)
-- ============================================
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],      -- 0 = Add, 1 = Edit, 2 = Delete
    [RequestedBy],      -- EmployeeID của RM
    [Code],             -- Mã giảm giá
    [Type],             -- 0=Percentage, 1=FixedAmount, 2=FreeShipping, ...
    [Percent],          -- Phần trăm giảm (nếu dùng Percentage)
    [Amount],           -- Số tiền giảm (nếu dùng FixedAmount)
    [StartAt],          -- Ngày bắt đầu
    [EndAt],            -- Ngày kết thúc
    [IsActive],         -- 1 = Hoạt động, 0 = Vô hiệu
    [UsageLimit]        -- Giới hạn số lần sử dụng (NULL = không giới hạn)
)
VALUES (
    0,                              -- RequestType: Add
    'RM001',                        -- ⚠️ Thay bằng EmployeeID thực tế
    'SALE2024',                     -- Mã giảm giá
    0,                              -- Type: Percentage
    10.00,                          -- Giảm 10%
    NULL,                           -- Amount: NULL nếu dùng Percentage
    '2024-01-01 00:00:00',         -- Ngày bắt đầu
    '2024-12-31 23:59:59',         -- Ngày kết thúc
    1,                              -- IsActive: True
    100                             -- Giới hạn 100 lần
);

-- ============================================
-- INSERT YÊU CẦU SỬA MÃ GIẢM GIÁ (Edit)
-- ============================================
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],      -- ⚠️ ID của Discount cần sửa
    [RequestedBy],
    [Code],             -- Code mới
    [Type],
    [Percent],
    [Amount],
    [StartAt],
    [EndAt],
    [IsActive],
    [UsageLimit]
)
VALUES (
    1,                              -- RequestType: Edit
    1,                              -- ⚠️ ID của Discount cần sửa
    'RM001',                        -- ⚠️ Thay bằng EmployeeID thực tế
    'SALE2024_UPDATED',             -- Code mới
    0,                              -- Type: Percentage
    15.00,                          -- Percent mới: 15%
    NULL,
    '2024-01-01 00:00:00',
    '2024-12-31 23:59:59',
    1,
    150                             -- UsageLimit mới
);

-- ============================================
-- INSERT YÊU CẦU XÓA MÃ GIẢM GIÁ (Delete)
-- ============================================
INSERT INTO [dbo].[DiscountRequest] (
    [RequestType],
    [DiscountId],      -- ⚠️ ID của Discount cần xóa
    [RequestedBy],
    [Code]             -- Code của Discount cần xóa (để Admin biết)
)
VALUES (
    2,                              -- RequestType: Delete
    2,                              -- ⚠️ ID của Discount cần xóa
    'RM001',                        -- ⚠️ Thay bằng EmployeeID thực tế
    'OLD_CODE'                      -- Code của Discount cần xóa
);

-- ============================================
-- LƯU Ý:
-- ============================================
-- 1. Thay 'RM001' bằng EmployeeID thực tế của RM
-- 2. Thay DiscountId bằng ID thực tế khi Edit hoặc Delete
-- 3. Status mặc định là 0 (Pending) - không cần điền
-- 4. ReviewedBy, ReviewedAt, RejectionReason sẽ được Admin điền khi duyệt
-- 5. RequestedAt sẽ tự động lấy GETUTCDATE() nếu không điền


















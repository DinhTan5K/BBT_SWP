# Hướng dẫn sử dụng Custom Dropdown trong Admin Panel

## Tự động áp dụng

Custom dropdown **tự động áp dụng** cho tất cả các `<select>` trong Admin Panel mà không cần thêm code gì!

### Các select được tự động convert:

1. ✅ Tất cả `<select>` trong `.admin-filter-select` (filter dropdowns)
2. ✅ Tất cả `<select>` trong `<main>` (main content area)
3. ✅ Tất cả `<select>` trong `.card` (card containers)

### Ví dụ - Đã hoạt động tự động:

```html
<!-- Discounts.cshtml - Đã có class admin-filter-select -->
<div class="admin-filter-select">
    <select id="filterType" onchange="filterByType()">
        <option value="Tất cả">Tất cả</option>
        <option value="Sale">Sale</option>
        <option value="Ship">Ship</option>
    </select>
</div>

<!-- Orders.cshtml - Đã có class admin-filter-select -->
<div class="admin-filter-select">
    <select name="status">
        <option value="">Tất cả</option>
        <option value="Pending">Chờ duyệt</option>
    </select>
</div>

<!-- Approvals.cshtml - Đã có class admin-filter-select -->
<div class="admin-filter-select">
    <select id="statusFilter" onchange="filterByStatus()">
        <option value="Pending">Chờ duyệt</option>
        <option value="Approved">Đã duyệt</option>
    </select>
</div>
```

## Cách thêm vào page mới

### Cách 1: Sử dụng class `admin-filter-select` (Khuyến nghị)

```html
<div class="admin-filter-select">
    <select id="mySelect" onchange="myFunction()">
        <option value="1">Option 1</option>
        <option value="2">Option 2</option>
        <option value="3">Option 3</option>
    </select>
</div>
```

### Cách 2: Đặt trong `<main>` hoặc `.card`

```html
<main>
    <div class="card">
        <select name="category">
            <option value="">Chọn danh mục</option>
            <option value="1">Danh mục 1</option>
        </select>
    </div>
</main>
```

## Bỏ qua custom dropdown (nếu cần)

Nếu bạn muốn giữ nguyên select mặc định của browser:

### Cách 1: Thêm data attribute

```html
<select data-skip-custom-dropdown>
    <option>Option 1</option>
</select>
```

### Cách 2: Sử dụng class `form-select` trong modal

```html
<div class="modal">
    <select class="form-select">
        <option>Option 1</option>
    </select>
</div>
```

## Tính năng

- ✅ Tự động convert tất cả select
- ✅ Giữ nguyên functionality (onchange events vẫn hoạt động)
- ✅ Click outside để đóng
- ✅ Keyboard support (Escape để đóng)
- ✅ Animation mượt mà
- ✅ Responsive design
- ✅ Style đẹp với theme admin

## CSS Classes có sẵn

Nếu muốn style riêng, bạn có thể dùng:

- `.admin-custom-dropdown` - Container
- `.admin-custom-dropdown-trigger` - Button trigger
- `.admin-custom-dropdown-options` - Options container
- `.admin-custom-dropdown-option` - Mỗi option
- `.admin-custom-dropdown-option.selected` - Option được chọn
- `.admin-custom-dropdown.active` - Khi dropdown mở

## Lưu ý

- JavaScript tự động load trong `_AdminLayout.cshtml`
- Không cần thêm code JavaScript cho từng page
- Tất cả select trong admin panel sẽ tự động được convert


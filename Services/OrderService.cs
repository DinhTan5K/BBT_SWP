using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using start.Data;
using start.Models;
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private static readonly Random _random = new Random();

    public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<string> GenerateUniqueOrderCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string orderCode;
        do
        {
            orderCode = new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
        // Kiểm tra trong DB để đảm bảo mã là duy nhất
        while (await _context.Orders.AnyAsync(o => o.OrderCode == orderCode));

        return orderCode;
    }

    public async Task<(bool success, string message, int? orderId)> CreateOrderAsync(int customerId, OrderFormModel form)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra các điều kiện đầu vào
            var branch = await _context.Branches.FindAsync(form.BranchID);
            if (branch == null)
                return (false, "Chi nhánh không tồn tại", null);

            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return (false, "Khách hàng không tồn tại", null);

            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.CustomerID == customer.CustomerID);

            if (cart == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống", null);

            // 2. Chuẩn bị dữ liệu Order và OrderDetail (chưa lưu vào DB)
            var order = new Order
            {
                CustomerID = customer.CustomerID,
                BranchID = form.BranchID,
                CreatedAt = DateTime.Now,
                Status = "Chờ xác nhận",
                OrderCode = await GenerateUniqueOrderCodeAsync(),
                Address = form.Address,
                DetailAddress = form.DetailAddress,
                NoteOrder = form.Note,
                ReceiverName = form.Name,
                ReceiverPhone = form.Phone,
                PaymentMethod = string.IsNullOrWhiteSpace(form.Payment) ? null : form.Payment.Trim(),
                ShippingFee = form.ShippingFee,
                PromoCode = string.IsNullOrWhiteSpace(form.PromoCode) ? null : form.PromoCode.Trim().ToUpper(),
                Total = 0 // Sẽ được tính toán lại
            };

            var orderDetails = cart.CartDetails.Select(cd => new OrderDetail
            {
                Order = order, // Gán trực tiếp đối tượng, EF sẽ tự hiểu
                ProductID = cd.ProductID,
                ProductSizeID = cd.ProductSizeID,
                Quantity = cd.Quantity,
                UnitPrice = cd.UnitPrice,
                Total = cd.UnitPrice * cd.Quantity,
            }).ToList();

            // 3. Tính toán tổng tiền
            var itemsTotal = orderDetails.Sum(d => d.Total);

            // Gọi hàm tính toán giảm giá đã được tái sử dụng
            var calculationResult = await CalculateDiscountAsync(form.PromoCode, itemsTotal, form.ShippingFee);
            _logger.LogInformation("[OrderService] CalculateDiscount: PromoInput='{Promo}', ItemsTotal={ItemsTotal}, ShippingFeeInput={ShipInput}, FinalTotal={FinalTotal}, FinalShippingFee={FinalShip}, AppliedCodes={Applied}",
                form.PromoCode,
                itemsTotal,
                form.ShippingFee,
                calculationResult.FinalTotal,
                calculationResult.FinalShippingFee,
                string.Join(',', calculationResult.SuccessfullyAppliedCodes ?? new List<string>()));
            // Kiểm tra lỗi tính toán giảm giá (nếu có mã bị trả về lỗi)
            if (calculationResult.ErrorMessage != null)
            {
                // Trả về lỗi nếu mã không hợp lệ/hết hạn
                return (false, calculationResult.ErrorMessage, null);
            }

            // Cập nhật tổng tiền cuối cùng cho đơn hàng
            order.Total = calculationResult.FinalTotal;
            order.ShippingFee = calculationResult.FinalShippingFee;

            // Gộp các mã đã áp dụng với các mã giảm phí ship hợp lệ user gửi (kể cả khi ship = 0)
            var appliedList = calculationResult.SuccessfullyAppliedCodes?.ToList() ?? new List<string>();
            var requestedCodes = string.IsNullOrWhiteSpace(form.PromoCode)
                ? new List<string>()
                : form.PromoCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(c => c.ToUpper())
                    .Distinct()
                    .ToList();

            if (requestedCodes.Any())
            {
                var nowForOrder = DateTime.Now;
                var requestedDiscounts = await _context.Discounts
                    .Where(d => requestedCodes.Contains(d.Code)
                                && d.IsActive
                                && (d.StartAt == null || d.StartAt <= nowForOrder)
                                && (d.EndAt == null || d.EndAt >= nowForOrder))
                    .ToListAsync();

                var shippingTypes = new[] { DiscountType.FreeShipping, DiscountType.FixedShippingDiscount, DiscountType.PercentShippingDiscount };
                var requestedShippingCodes = requestedDiscounts
                    .Where(d => shippingTypes.Contains(d.Type))
                    .Select(d => d.Code)
                    .ToList();

                foreach (var code in requestedShippingCodes)
                {
                    if (!appliedList.Contains(code)) appliedList.Add(code);
                }
            }

            order.PromoCode = appliedList.Any()
                ? string.Join(",", appliedList)
                : (string.IsNullOrWhiteSpace(form.PromoCode) ? null : form.PromoCode.Trim().ToUpper());

            // Log lại giá trị sẽ lưu
            _logger.LogInformation("[OrderService] PersistOrder: PromoInput='{Promo}', SavedPromo='{Saved}', FinalShip={FinalShip}, FinalTotal={FinalTotal}",
                form.PromoCode,
                order.PromoCode,
                order.ShippingFee,
                order.Total);


            // 4. Thêm tất cả vào Context và dọn dẹp giỏ hàng
            _context.Orders.Add(order);
            _context.OrderDetails.AddRange(orderDetails);
            _context.CartDetails.RemoveRange(cart.CartDetails);

            // 5. Lưu tất cả thay đổi vào DB trong một lần duy nhất
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Tạo order thành công", order.OrderID);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Ghi log lỗi ở đây nếu cần (ví dụ: logger.LogError(ex, "Lỗi khi tạo đơn hàng");)
            return (false, "Đã có lỗi xảy ra: " + ex.Message, null);
        }
    }

    public async Task<PromoCodeResponse> CalculateDiscountAsync(string promoCodes, decimal itemsTotal, decimal shippingFee)
    {
        var response = new PromoCodeResponse
        {
            AppliedMessages = new List<string>(),
            // Thêm trường này vào PromoCodeResponse để lưu các mã thành công
            SuccessfullyAppliedCodes = new List<string>()
        };
        // 1. CHUẨN BỊ VÀ LỌC MÃ
        var codeList = new List<string>();
        if (!string.IsNullOrWhiteSpace(promoCodes))
        {
            codeList = promoCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.ToUpper()).Distinct().ToList();
        }


        // Khởi tạo các biến tính toán
        decimal currentItemsTotal = itemsTotal;
        decimal totalDiscountAmount = 0;
        decimal finalShippingFee = shippingFee;
        bool shippingDiscountApplied = false;
        Discount mainItemDiscountApplied = null;

        var now = DateTime.Now;
        var validDiscounts = codeList.Any()
            ? await _context.Discounts
                .Where(d => d.IsActive && codeList.Contains(d.Code)
                            && (d.StartAt == null || d.StartAt <= now)
                            && (d.EndAt == null || d.EndAt >= now))
                .ToListAsync()
            : new List<Discount>();
        // 2. XỬ LÝ LỖI MÃ KHÔNG HỢP LỆ (Lọc ra những mã bị lỗi thời gian/active)
        var appliedCodeList = validDiscounts.Select(d => d.Code).ToList();
        var firstTrulyInvalidCode = codeList.FirstOrDefault(c => !appliedCodeList.Contains(c));

        if (firstTrulyInvalidCode != null)
        {
            response.ErrorMessage = $"Mã '{firstTrulyInvalidCode}' không hợp lệ hoặc đã hết hạn.";
            response.FinalTotal = itemsTotal + shippingFee;
            response.FinalShippingFee = shippingFee;
            return response;
        }


        // 3. ÁP DỤNG LOGIC GIẢM GIÁ ĐẦY ĐỦ (FixedAmount, FixedShipping, PercentShipping)
        foreach (var discount in validDiscounts.OrderBy(d => (int)d.Type))
        {
            switch (discount.Type)
            {
                case DiscountType.Percentage:
                case DiscountType.FixedAmount: // <-- Xử lý mã GIAM50K
                    if (mainItemDiscountApplied == null)
                    {
                        mainItemDiscountApplied = discount;
                        decimal discountValue = (discount.Type == DiscountType.Percentage)
                            ? Math.Round(currentItemsTotal * (discount.Percent / 100.0m), 0)
                            : discount.Amount ?? 0;

                        // Giảm giá không được vượt quá tiền hàng
                        discountValue = Math.Min(discountValue, currentItemsTotal);

                        totalDiscountAmount += discountValue;
                        currentItemsTotal -= discountValue; // ✅ TRỪ TRỰC TIẾP TỪ TIỀN HÀNG

                        response.AppliedMessages.Add(discount.Type == DiscountType.Percentage
                            ? $"✅ Áp dụng giảm giá {discount.Percent}%."
                            : $"✅ Giảm giá {discountValue.ToString("#,0")} đ.");

                        response.SuccessfullyAppliedCodes.Add(discount.Code);
                    }
                    break;

                case DiscountType.FreeShipping:
                    if (!shippingDiscountApplied && finalShippingFee > 0)
                    {
                        totalDiscountAmount += finalShippingFee;
                        finalShippingFee = 0;
                        shippingDiscountApplied = true;
                        response.AppliedMessages.Add("✅ Miễn phí vận chuyển đã được áp dụng.");
                        response.SuccessfullyAppliedCodes.Add(discount.Code);
                    }
                    break;

                case DiscountType.FixedShippingDiscount: // <-- Xử lý mã SHIP20K
                    if (!shippingDiscountApplied && finalShippingFee > 0)
                    {
                        var fixedDiscount = discount.Amount ?? 0;
                        var discountAmount = Math.Min(fixedDiscount, finalShippingFee);

                        totalDiscountAmount += discountAmount;
                        finalShippingFee -= discountAmount;
                        shippingDiscountApplied = true;

                        response.AppliedMessages.Add($"✅ Giảm {discountAmount.ToString("#,0")} đ phí vận chuyển.");
                        response.SuccessfullyAppliedCodes.Add(discount.Code);
                    }
                    break;

                case DiscountType.PercentShippingDiscount: // <-- Xử lý mã SHIP15P
                    if (!shippingDiscountApplied && finalShippingFee > 0)
                    {
                        var percent = discount.Percent / 100.0m;
                        var calculatedDiscount = Math.Round(finalShippingFee * percent);
                        var discountAmount = Math.Min(calculatedDiscount, finalShippingFee);

                        totalDiscountAmount += discountAmount;
                        finalShippingFee -= discountAmount;
                        shippingDiscountApplied = true;

                        response.AppliedMessages.Add($"✅ Giảm {discount.Percent}% phí vận chuyển.");
                        response.SuccessfullyAppliedCodes.Add(discount.Code);
                    }
                    break;
            }
        }

        // 4. KẾT QUẢ CUỐI CÙNG
        finalShippingFee = Math.Max(0m, finalShippingFee);

        response.FinalShippingFee = finalShippingFee;
        response.TotalDiscountAmount = totalDiscountAmount;

        // Tổng cuối cùng = Tiền hàng SAU giảm + Phí ship CUỐI CÙNG
        response.FinalTotal = currentItemsTotal + finalShippingFee;

        return response;
    }


    public async Task<PromoValidationResult> ValidateAndApplyPromoCodesAsync(PromoValidationRequest request)
    {
        var result = new PromoValidationResult();

        // 🔹 Nếu không có mã nào thì return mặc định
        if (request?.Codes == null || !request.Codes.Any())
        {
            result.FinalTotal = request.ItemsTotal + request.ShippingFee;
            result.FinalShippingFee = request.ShippingFee;
            result.TotalDiscount = 0;
            return result;
        }

        // 🔹 Khai báo biến dùng chung
        decimal currentItemsTotal = request.ItemsTotal;
        decimal totalDiscountAmount = 0;
        decimal finalShippingFee = request.ShippingFee;

        bool freeShipApplied = false;
        var appliedMessages = new List<string>();
        var successfullyAppliedCodes = new List<string>();
        var shippingCodes = new List<string>();

        var now = DateTime.Now;
        var distinctCodes = request.Codes
            .Distinct()
            .Select(c => c.ToUpper().Trim())
            .ToList();

        // 🔹 Lấy danh sách giảm giá từ DB
        var discountsFromDb = await _context.Discounts
            .Where(d => distinctCodes.Contains(d.Code))
            .ToListAsync();

        // 🔹 Kiểm tra thời gian hiệu lực & active
        var validDiscounts = discountsFromDb
            .Where(d => d.IsActive && now >= d.StartAt && now <= d.EndAt)
            .ToList();

        var validDiscountCodes = validDiscounts.Select(d => d.Code).ToList();
        var firstInvalid = distinctCodes.FirstOrDefault(c => !validDiscountCodes.Contains(c));

        if (firstInvalid != null)
        {
            result.ErrorMessage = $"Mã '{firstInvalid}' không hợp lệ hoặc đã hết hạn.";
            result.InvalidCode = firstInvalid;
            return result;
        }

        // 🔹 Áp dụng logic tính toán
        Discount mainDiscountApplied = null;

        foreach (var discount in validDiscounts.OrderBy(d => d.Type))
        {
            switch (discount.Type)
            {
                // ====== MÃ GIẢM GIÁ CHÍNH ======
                case DiscountType.Percentage:
                case DiscountType.FixedAmount:
                    if (mainDiscountApplied == null)
                    {
                        mainDiscountApplied = discount;
                        decimal currentDiscount = 0;

                        if (discount.Type == DiscountType.Percentage)
                        {
                            currentDiscount = request.ItemsTotal * (discount.Percent / 100.0m);
                            appliedMessages.Add($"✅ Áp dụng giảm giá {discount.Percent}%.");
                        }
                        else
                        {
                            currentDiscount = discount.Amount ?? 0;
                            appliedMessages.Add($"✅ Giảm giá {currentDiscount.ToString("#,0")} đ.");
                        }

                        // Không cho phép giảm quá tổng tiền hàng
                        currentDiscount = Math.Min(currentDiscount, currentItemsTotal);

                        totalDiscountAmount += currentDiscount;
                        currentItemsTotal -= currentDiscount;

                        successfullyAppliedCodes.Add(discount.Code);
                    }
                    else
                    {
                        result.ErrorMessage = $"Chỉ có thể dùng 1 mã giảm giá chính (loại % hoặc tiền). Vui lòng gỡ mã '{discount.Code}' hoặc '{mainDiscountApplied.Code}'.";
                        result.InvalidCode = discount.Code;
                        return result;
                    }
                    break;

                // ====== FREESHIP ======
                case DiscountType.FreeShipping:
                    if (freeShipApplied)
                    {
                        result.ErrorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.";
                        result.InvalidCode = discount.Code;
                        result.CurrentShippingCode = shippingCodes.FirstOrDefault();
                        return result;
                    }
                    if (request.ShippingFee > 0)
                    {
                        totalDiscountAmount += finalShippingFee;
                        finalShippingFee = 0;
                        appliedMessages.Add("✅ Áp dụng miễn phí vận chuyển.");
                        successfullyAppliedCodes.Add(discount.Code);
                        freeShipApplied = true;
                        shippingCodes.Add(discount.Code);
                    }
                    break;

                // ====== GIẢM PHÍ SHIP CỐ ĐỊNH ======
                case DiscountType.FixedShippingDiscount:
                    if (freeShipApplied)
                    {
                        result.ErrorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.";
                        result.InvalidCode = discount.Code;
                        result.CurrentShippingCode = shippingCodes.FirstOrDefault();
                        return result;
                    }
                    if (request.ShippingFee > 0)
                    {
                        var fixedDiscount = discount.Amount ?? 0;
                        var discountAmount = Math.Min(fixedDiscount, finalShippingFee);

                        totalDiscountAmount += discountAmount;
                        finalShippingFee -= discountAmount;

                        appliedMessages.Add($"✅ Giảm {discountAmount.ToString("#,0")} đ phí vận chuyển.");
                        successfullyAppliedCodes.Add(discount.Code);
                        freeShipApplied = true;
                        shippingCodes.Add(discount.Code);
                    }
                    break;

                // ====== GIẢM PHÍ SHIP THEO % ======
                case DiscountType.PercentShippingDiscount:
                    if (freeShipApplied)
                    {
                        result.ErrorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.";
                        result.InvalidCode = discount.Code;
                        result.CurrentShippingCode = shippingCodes.FirstOrDefault();
                        return result;
                    }
                    if (request.ShippingFee > 0)
                    {
                        var percent = discount.Percent / 100.0m;
                        var calculatedDiscount = Math.Round(finalShippingFee * percent);
                        var discountAmount = Math.Min(calculatedDiscount, finalShippingFee);

                        totalDiscountAmount += discountAmount;
                        finalShippingFee -= discountAmount;

                        appliedMessages.Add($"✅ Giảm {discount.Percent}% phí vận chuyển.");
                        successfullyAppliedCodes.Add(discount.Code);
                        freeShipApplied = true;
                        shippingCodes.Add(discount.Code);
                    }
                    break;
            }
        }

        // ✅ Đảm bảo phí ship không âm
        finalShippingFee = Math.Max(0, finalShippingFee);

        // ✅ Tính tổng cuối cùng
        result.FinalTotal = currentItemsTotal + finalShippingFee;
        result.FinalShippingFee = finalShippingFee;
        result.TotalDiscount = totalDiscountAmount;
        result.AppliedMessages = appliedMessages;
        result.SuccessfullyAppliedCodes = successfullyAppliedCodes;

        return result;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product!)
            .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.ProductSize)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderID == id);
    }

    public async Task<object?> GetOrderByCodeAsync(string orderCode)
    {
        return await _context.Orders
            .Where(o => o.OrderCode == orderCode)
            .Select(o => new
            {
                o.OrderID,
                o.CustomerID,
                o.CreatedAt,
                o.OrderCode,
                o.Status,
                o.Total,
                o.DetailAddress,
                o.NoteOrder,
                o.ShippingFee,
                o.PromoCode,
                o.Address,
                o.ReceiverName,
                o.ReceiverPhone,
                o.PaymentMethod
            })
            .FirstOrDefaultAsync();
    }
}

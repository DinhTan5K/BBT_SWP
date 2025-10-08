 [HttpPost("ValidatePromoCodes")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ValidatePromoCodes([FromBody] PromoValidationRequest request)
    {
        // Kiểm tra trường hợp không có mã nào được áp dụng
        if (request?.Codes == null || !request.Codes.Any())
        {
            return Json(new
            {
                finalTotal = request.ItemsTotal + request.ShippingFee,
                finalShippingFee = request.ShippingFee,
                totalDiscount = 0
            });
        }

        // KHAI BÁO BIẾN DÙNG CHUNG (KHÔNG KHAI BÁO LẠI BÊN DƯỚI)
        decimal currentItemsTotal = request.ItemsTotal; // sẽ trừ dần với mã giảm trên tiền hàng
        decimal totalDiscountAmount = 0; // tổng giảm để hiển thị (bao gồm item + shipping)
        decimal finalShippingFee = request.ShippingFee;
        var appliedMessages = new List<string>();
        var successfullyAppliedCodes = new List<string>();
        bool freeShipApplied = false; // Dùng để kiểm tra mã freeship/giảm phí ship đã được áp dụng
        var shippingCodes = new List<string>(); // Lưu thứ tự các mã ship người dùng nhập

        var now = DateTime.Now;
        var distinctCodes = request.Codes.Distinct().Select(c => c.ToUpper().Trim()).ToList();

        var discountsFromDb = await _context.Discounts
            .Where(d => distinctCodes.Contains(d.Code))
            .ToListAsync();

        var validDiscounts = discountsFromDb
            .Where(d => d.IsActive && now >= d.StartAt && now <= d.EndAt)
            .ToList();

        var validDiscountCodes = validDiscounts.Select(d => d.Code).ToList();
        var firstTrulyInvalidCode = distinctCodes.FirstOrDefault(c => !validDiscountCodes.Contains(c));

        if (firstTrulyInvalidCode != null)
        {
            return Json(new
            {
                errorMessage = $"Mã '{firstTrulyInvalidCode}' không hợp lệ hoặc đã hết hạn.",
                invalidCode = firstTrulyInvalidCode
            });
        }

        // ================= LOGIC XỬ LÝ GIẢM GIÁ =================

        Discount mainDiscountApplied = null; // Dùng để kiểm tra mã giảm giá chính (tiền hoặc %)

        // Sắp xếp để ưu tiên xử lý mã giảm giá chính trước (giả định DiscountType có thể sắp xếp)
        foreach (var discount in validDiscounts.OrderBy(d => d.Type))
        {
            switch (discount.Type)
            {
                // Cả hai case này đều được coi là "mã giảm giá chính"
                case DiscountType.Percentage:
                case DiscountType.FixedAmount:
                    if (mainDiscountApplied == null)
                    {
                        mainDiscountApplied = discount;
                        decimal currentDiscount = 0;

                        if (discount.Type == DiscountType.Percentage)
                        {
                            // Giả định Percent là thuộc tính cho loại Percentage
                            currentDiscount = request.ItemsTotal * (discount.Percent / 100.0m);
                            appliedMessages.Add($"✅ Áp dụng giảm giá {discount.Percent}%.");
                        }
                        else // FixedAmount
                        {
                            // Giả định Amount là thuộc tính cho loại FixedAmount
                            currentDiscount = discount.Amount ?? 0;
                            appliedMessages.Add($"✅ Giảm giá {currentDiscount.ToString("#,0")} đ.");
                        }

                        // Không cho phép giảm quá tổng tiền hàng
                        currentDiscount = Math.Min(currentDiscount, currentItemsTotal);
                        totalDiscountAmount += currentDiscount;
                        currentItemsTotal -= currentDiscount; // trừ trực tiếp vào tiền hàng
                        successfullyAppliedCodes.Add(discount.Code);
                    }
                    else
                    {
                        // Đã có mã giảm giá chính, báo lỗi xung đột
                        return Json(new
                        {
                            errorMessage = $"Chỉ có thể dùng 1 mã giảm giá chính (loại % hoặc tiền). Vui lòng gỡ mã '{discount.Code}' hoặc '{mainDiscountApplied.Code}'.",
                            invalidCode = discount.Code
                        });
                    }
                    break;

                case DiscountType.FreeShipping:
                    // Chỉ cho phép 1 mã ship
                    if (freeShipApplied)
                    {
                        return Json(new
                        {
                            errorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.",
                            invalidCode = discount.Code,
                            currentShippingCode = shippingCodes.FirstOrDefault()
                        });
                    }
                    if (request.ShippingFee > 0)
                    {
                        totalDiscountAmount += finalShippingFee; // Cộng phí ship hiện tại vào tổng giảm
                        finalShippingFee = 0;
                        appliedMessages.Add("✅ Áp dụng miễn phí vận chuyển.");
                        successfullyAppliedCodes.Add(discount.Code);
                        freeShipApplied = true;
                        shippingCodes.Add(discount.Code);
                    }
                    break;

                case DiscountType.FixedShippingDiscount:
                    if (freeShipApplied)
                    {
                        return Json(new
                        {
                            errorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.",
                            invalidCode = discount.Code,
                            currentShippingCode = shippingCodes.FirstOrDefault()
                        });
                    }
                    if (request.ShippingFee > 0)
                    {
                        var fixedDiscountAmount = discount.Amount ?? 0;

                        // Tính toán số tiền giảm, không giảm quá phí ship hiện tại
                        var discountAmount = Math.Min(fixedDiscountAmount, finalShippingFee);

                        totalDiscountAmount += discountAmount;
                        finalShippingFee -= discountAmount;

                        appliedMessages.Add($"✅ Giảm {discountAmount.ToString("#,0")} đ phí vận chuyển.");
                        successfullyAppliedCodes.Add(discount.Code);
                        freeShipApplied = true;
                        shippingCodes.Add(discount.Code);
                    }
                    break;

                case DiscountType.PercentShippingDiscount:
                    // Chỉ cho phép 1 mã ship
                    if (freeShipApplied)
                    {
                        return Json(new
                        {
                            errorMessage = $"Chỉ có thể dùng 1 mã giảm phí vận chuyển. Vui lòng gỡ bỏ mã '{discount.Code}' hoặc '{shippingCodes.FirstOrDefault()}'.",
                            invalidCode = discount.Code,
                            currentShippingCode = shippingCodes.FirstOrDefault()
                        });
                    }
                    if (request.ShippingFee > 0)
                    {
                        var percent = discount.Percent / 100.0m;
                        var calculatedDiscount = Math.Round(finalShippingFee * percent);

                        // Áp dụng mức giảm (không giảm quá phí ship hiện tại)
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

        // Đảm bảo finalShippingFee không bao giờ âm
        finalShippingFee = Math.Max(0, finalShippingFee);

        // Tính tổng cuối cùng theo service: (tiền hàng sau giảm) + (phí ship cuối)
        decimal finalTotalSuccess = currentItemsTotal + finalShippingFee;

        // Trả về kết quả
        return Json(new
        {
            finalTotal = finalTotalSuccess,
            finalShippingFee,
            totalDiscount = totalDiscountAmount,
            appliedMessages,
            successfullyAppliedCodes
        });
    }
}
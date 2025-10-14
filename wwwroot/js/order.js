document.addEventListener("DOMContentLoaded", () => {
    // --- STATE & CONFIG ---
    let appliedCodes = [];
    let branches = [];
    let userPosition = null;
    let currentShippingFee = 0;
    let isDeliveryMode = true;
    const SHIPPING_RATE = 5000;
    let hasPromoError = false; // block submit when promo validation has error

    // --- TOAST HELPER ---
    function showToast(message, type = 'danger') {
        // type: 'success' | 'danger' | 'warning' | 'info'
        const bg = type === 'success' ? '#d1e7dd' : type === 'warning' ? '#fff3cd' : type === 'info' ? '#cff4fc' : '#f8d7da';
        const color = '#000';
        const border = type === 'success' ? '#badbcc' : type === 'warning' ? '#ffecb5' : type === 'info' ? '#b6effb' : '#f5c2c7';

        const toast = document.createElement('div');
        toast.setAttribute('role', 'alert');
        toast.style.position = 'fixed';
        toast.style.top = '70px';
        toast.style.right = '10px';
        toast.style.zIndex = '1050';
        toast.style.maxWidth = '420px';
        toast.style.background = bg;
        toast.style.color = color;
        toast.style.border = `1px solid ${border}`;
        toast.style.borderRadius = '6px';
        toast.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
        toast.style.padding = '12px 16px';
        toast.style.fontSize = '14px';
        toast.style.lineHeight = '1.4';
        toast.style.transition = 'transform .4s ease, opacity .4s ease';
        toast.style.transform = 'translateX(120%)';
        toast.style.opacity = '0';
        toast.innerHTML = message;

        document.body.appendChild(toast);
        requestAnimationFrame(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        });

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(120%)';
            setTimeout(() => toast.remove(), 800);
        }, 3000);
    }

    // --- DOM ELEMENTS ---
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const checkoutForm = document.getElementById('checkoutForm');
    const steps = document.querySelectorAll(".checkout-steps .step");
    // Promo
    const promoInput = document.getElementById("promoCodeInput");
    const applyBtn = document.getElementById("btnApplyPromo");
    const promoSpinner = applyBtn.querySelector(".spinner-border");
    const codesContainer = document.getElementById("appliedCodesContainer");
    const promoMsg = document.getElementById("promoMessage");
    // Summary
    const itemsTotalEl = document.getElementById("itemsTotal");
    const shippingFeeEl = document.getElementById("shippingFee");
    const discountRow = document.getElementById("discountRow");
    const discountAmountEl = document.getElementById("discountAmount");
    const grandTotalEl = document.getElementById("grandTotal");
    // Delivery Form
    const nameInput = document.getElementById("nameInput");
    const phoneInput = document.getElementById("phoneInput");
    const addressInput = document.getElementById("addressInput");
    const detailAddressInput = document.getElementById("detailAddress");
    const btnLocation = document.getElementById("btnLocation");
    const nearestBranchInput = document.getElementById("nearestBranchInput");
    const branchWarning = document.getElementById("branchWarning");
    const branchIdInput = document.getElementById("branchIdInput");
    // Pickup Form
    const branchSelect = document.getElementById("branchSelect");
    const pickupNameInput = document.getElementById("pickupNameInput");
    const pickupPhoneInput = document.getElementById("pickupPhoneInput");
    // Submit Button
    const btnSubmitOrder = document.getElementById("btnSubmitOrder");
    const orderNoteDeliveryInput = document.getElementById("orderNoteDelivery");
    const orderNotePickupInput = document.getElementById("orderNotePickup");
    const orderNoteInput = document.getElementById("orderNote");
    const submitSpinner = btnSubmitOrder.querySelector('.spinner-border');
    // --- STEP UI FUNCTIONS ---
    // --- STEP UI FUNCTIONS ---
    function setStep(stepIndex) {
        steps.forEach((step, index) => {
            step.classList.toggle("active", index <= stepIndex);
        });
    }

    function updateCheckoutStepUI() {
        const phoneRegex = /^(0[3|5|7|8|9])+([0-9]{8})\b/;
        const selectedPayment = document.querySelector('input[name="Payment"]:checked');
        let isInfoComplete = false;

        if (isDeliveryMode) {
            isInfoComplete =
                nameInput.value.trim() &&
                phoneRegex.test(phoneInput.value.trim()) &&
                addressInput.value.trim() &&
                branchIdInput.value;
        } else {
            isInfoComplete =
                pickupNameInput.value.trim() &&
                phoneRegex.test(pickupPhoneInput.value.trim()) &&
                branchSelect.value;
        }

        // Nếu đã nhập đủ info và có chọn payment → sang bước 2
        if (isInfoComplete && selectedPayment) {
            setStep(1); // step 2 (Payment)
        } else {
            setStep(0); // step 1 (Information)
        }
    }

    // Gắn event listener cho input thanh toán
    document.querySelectorAll('input[name="Payment"]').forEach(radio => {
        radio.addEventListener("change", updateCheckoutStepUI);
    });

    // Gọi lại hàm khi user nhập dữ liệu
    [nameInput, phoneInput, addressInput, pickupNameInput, pickupPhoneInput, branchSelect]
        .forEach(input => input?.addEventListener("input", updateCheckoutStepUI));


    // --- VALIDATION HELPER ---
    const errorFields = ['nameError', 'phoneError', 'addressError', 'detailError', 'branchWarning', 'pickupBranchError', 'pickupNameError', 'pickupPhoneError'];
    function showError(elementId, message) {
        const el = document.getElementById(elementId);
        if (el) { el.textContent = message; el.classList.remove('d-none'); }
    }
    function hideAllErrors() {
        errorFields.forEach(id => {
            const el = document.getElementById(id);
            if (el) el.classList.add('d-none');
        });
    }

    // --- CORE LOGIC (PROMO & SHIPPING) ---
    async function updateDiscountsAndRender() {
        applyBtn.disabled = true;
        promoSpinner.classList.remove("d-none");
        promoMsg.textContent = "";

        const requestData = {
            codes: appliedCodes,
            itemsTotal: parseFloat(itemsTotalEl.getAttribute('data-value')),
            shippingFee: isDeliveryMode ? currentShippingFee : 0
        };

        try {
            const response = await fetch('/Order/ValidatePromoCodes', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify(requestData)
            });
            const result = await response.json();

            if (result.errorMessage) {
                hasPromoError = true;
                // Show popup toast for error as well
                showToast(`❌ ${result.errorMessage}`, 'danger');
                // BUG FIX: On a conflict error, do NOT automatically remove the code.
                // Let the user decide which one to remove via the UI.
            } else {
                hasPromoError = false;
                const totalDiscount = result.totalDiscount || 0;
                discountRow.classList.toggle('d-none', totalDiscount <= 0);
                discountAmountEl.textContent = `- ${totalDiscount.toLocaleString('vi-VN')} đ`;
                grandTotalEl.textContent = `${result.finalTotal.toLocaleString('vi-VN')} đ`;
                shippingFeeEl.textContent = `${result.finalShippingFee.toLocaleString('vi-VN')} đ`;
                // Replace appliedMessages UI with toast notifications
                if (Array.isArray(result.appliedMessages)) {
                    result.appliedMessages.forEach(m => { if (m) showToast(m, 'success'); });
                }
                // Clear inline message area
                promoMsg.textContent = "";
                promoMsg.className = "mt-2 small";
            }
        } catch (error) {
            promoMsg.className = "mt-2 small text-danger";
            promoMsg.textContent = "❌ Có lỗi xảy ra, vui lòng thử lại.";
        } finally {
            applyBtn.disabled = false;
            promoSpinner.classList.add("d-none");
            renderAppliedCodeTags();
        }
    }

    function renderAppliedCodeTags() {
        codesContainer.innerHTML = appliedCodes.map(code => `
                                    <span class="promo-tag" data-code="${code}">
                                        ${code} <button type="button" class="btn-close btn-close-white" onclick="window.removePromoCode('${code}')"></button>
                                    </span>`).join("");
    }

    async function handleApplyCode() {
        const code = promoInput.value.trim().toUpperCase();
        if (!code) return;
        if (appliedCodes.includes(code)) {
            promoMsg.className = "mt-2 small text-warning";
            showToast(`⚠️ Mã "${code}" đã được áp dụng rồi.`, 'warning');
            return;
        }
        appliedCodes.push(code);
        promoInput.value = "";
        await updateDiscountsAndRender();
    }

    window.removePromoCode = async function (codeToRemove) {
        appliedCodes = appliedCodes.filter(c => c !== codeToRemove);
        await updateDiscountsAndRender();
    }

    async function loadBranches() {
        try {
            const res = await fetch('/Branch/GetAll');
            branches = await res.json();
            branchSelect.innerHTML = '<option value="">-- Chọn chi nhánh --</option>';
            branches.forEach(b => branchSelect.innerHTML += `<option value="${b.branchID}">${b.name} - ${b.address}</option>`);
        } catch (e) { console.error("Lỗi tải chi nhánh:", e); }
    }

    function updateShippingFee() {
        if (!userPosition || branches.length === 0) { currentShippingFee = 0; return; }
        branches.forEach(b => b.distance = calcDistance(userPosition.lat, userPosition.lon, b.latitude, b.longitude));
        branches.sort((a, b) => a.distance - b.distance);
        const nearest = branches[0];
        branchIdInput.value = nearest.branchID;
        nearestBranchInput.value = `${nearest.name} - ${nearest.distance.toFixed(2)} km`;

        if (nearest.distance > 15) {
            branchWarning.textContent = `Khoảng cách quá xa (>15km). Vui lòng chọn "Nhận tại cửa hàng".`;
            branchWarning.classList.remove('d-none');
            currentShippingFee = 0;
        } else {
            branchWarning.classList.add('d-none');
            currentShippingFee = Math.round(nearest.distance * SHIPPING_RATE);
        }
        shippingFeeEl.setAttribute('data-value', currentShippingFee);
        updateDiscountsAndRender();
    }

    // --- EVENT LISTENERS ---
    applyBtn.addEventListener("click", handleApplyCode);
    promoInput.addEventListener("keydown", e => e.key === "Enter" && (e.preventDefault(), handleApplyCode()));
    btnLocation.addEventListener("click", () => {
        if (!navigator.geolocation) return alert("Trình duyệt không hỗ trợ định vị.");
        navigator.geolocation.getCurrentPosition(async pos => {
            userPosition = { lat: pos.coords.latitude, lon: pos.coords.longitude };
            addressInput.value = 'Đang lấy địa chỉ...';
            updateShippingFee();
            try {
                const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${userPosition.lat}&lon=${userPosition.lon}`);
                const data = await res.json();
                addressInput.value = data?.display_name || 'Không tìm thấy địa chỉ';
            } catch (e) { console.error("Lỗi lấy địa chỉ:", e); }
        }, err => alert(`❌ Không thể lấy vị trí: ${err.message}`));
    });

    document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(tab => {
        tab.addEventListener('shown.bs.tab', e => {
            isDeliveryMode = (e.target.id === 'delivery-tab-link');
            updateDiscountsAndRender();
            updateCheckoutStepUI();
        });
    });

    const inputsToWatch = [nameInput, phoneInput, addressInput, pickupNameInput, pickupPhoneInput, branchSelect];
    inputsToWatch.forEach(input => {
        input.addEventListener('input', updateCheckoutStepUI);
    });

    btnSubmitOrder.addEventListener("click", async () => {
        hideAllErrors();
        let isValid = true;
        const phoneRegex = /^(0[3|5|7|8|9])+([0-9]{8})\b/;
        let branchIdValue = 0;

        if (isDeliveryMode) {
            if (!nameInput.value.trim()) { showError('nameError', 'Vui lòng nhập họ tên.'); isValid = false; }
            if (!phoneRegex.test(phoneInput.value.trim())) { showError('phoneError', 'Số điện thoại không hợp lệ.'); isValid = false; }
            if (!addressInput.value.trim()) { showError('addressError', 'Vui lòng nhập địa chỉ.'); isValid = false; }
            if (!detailAddressInput.value.trim()) { showError('detailError', 'Vui lòng nhập chi tiết địa chỉ.'); isValid = false; }
            if (!branchIdInput.value) { showError('branchWarning', 'Vui lòng chọn vị trí để tìm chi nhánh gần nhất.'); isValid = false; }
            branchIdValue = branchIdInput.value;
        } else { // Pickup Mode
            if (!pickupNameInput.value.trim()) { showError('pickupNameError', 'Vui lòng nhập họ tên.'); isValid = false; }
            if (!phoneRegex.test(pickupPhoneInput.value.trim())) { showError('pickupPhoneError', 'Số điện thoại không hợp lệ.'); isValid = false; }
            if (!branchSelect.value) { showError('pickupBranchError', 'Vui lòng chọn chi nhánh.'); isValid = false; }
            branchIdValue = branchSelect.value;
        }

        if (!isValid) return;

        btnSubmitOrder.disabled = true;
        submitSpinner.classList.remove('d-none');

        const formData = new FormData(checkoutForm);
        formData.set("BranchID", branchIdValue);

        // Prevent submit if promo validation currently has an error
        if (hasPromoError) {
            promoMsg.className = "mt-2 small text-danger";
            const extraNotice = "⚠️ Vui lòng xử lý lỗi mã khuyến mãi trước khi đặt hàng.";
            showToast(extraNotice, 'warning');
            btnSubmitOrder.disabled = false;
            submitSpinner.classList.add('d-none');
            return;
        }
        // Ensure only one Note value is sent
        formData.delete('Note');
        if (isDeliveryMode) {
            formData.set('Name', nameInput.value.trim());
            formData.set('Phone', phoneInput.value.trim());
            formData.set('Address', addressInput.value.trim());
            formData.set('BranchID', branchIdInput.value);
            formData.set('Note', (orderNoteDeliveryInput?.value || '').trim());
            formData.set('ShippingFee', String(currentShippingFee));
        } else {
            formData.set('Name', pickupNameInput.value.trim());
            formData.set('Phone', pickupPhoneInput.value.trim());
            formData.set('BranchID', branchSelect.value);
            const selectedBranchText = branchSelect.options[branchSelect.selectedIndex].text;
            formData.set('Address', `Nhận tại: ${selectedBranchText}`);
            formData.set('DetailAddress', '');
            formData.set('Note', (orderNotePickupInput?.value || '').trim());
            // Pickup has no shipping fee
            formData.set('ShippingFee', '0');
        }
        formData.append("PromoCode", appliedCodes.join(','));
        const paymentMethod = document.querySelector('input[name="Payment"]:checked')?.value || 'COD';
        try {
            const response = await fetch('/Order/CreateOrder', {
                method: 'POST',
                body: formData,
                headers: { 'RequestVerificationToken': token }
            });
            const result = await response.json();
            if (result.success) {
                if (paymentMethod === 'Momo') {
                    // Với MoMo: backend trả requireMomo, dùng trang GET để chuyển tiếp tới MoMo
                    window.location.href = `/Order/PayWithMomo`;
                } else {
                    // COD → hiển thị trang xác nhận đơn hàng
                    window.location.href = `/Order/Confirmed/${result.orderId}`;
                }
            } else {
                alert(`❌ Đặt hàng thất bại: ${result.message || 'Lỗi không xác định.'}`);
            }
        } catch (error) {
            alert('❌ Đã xảy ra lỗi kết nối. Vui lòng thử lại.');
        } finally {
            btnSubmitOrder.disabled = false;
            submitSpinner.classList.add('d-none');
        }
    });

    // --- UTILITY ---
    const calcDistance = (lat1, lon1, lat2, lon2) => {
        const R = 6371;
        const dLat = (lat2 - lat1) * Math.PI / 180; const dLon = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) ** 2 + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLon / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    };

    // --- INITIALIZATION ---
    loadBranches();
    updateDiscountsAndRender();
    updateCheckoutStepUI();
});
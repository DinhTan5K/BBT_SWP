document.addEventListener("DOMContentLoaded", () => {
    let branches = [];
    let appliedCodes = [];
    let userPosition = null;
    let currentShippingFee = 0;
    let isDeliveryMode = true;
    const SHIPPING_RATE = 5000;
    let hasPromoError = false;

    // --- TOAST HELPER ---
    function showToast(message, type = 'danger') {
        const bg = type === 'success' ? '#d1e7dd' : type === 'warning' ? '#fff3cd' : type === 'info' ? '#cff4fc' : '#f8d7da';
        const color = '#000';
        const border = type === 'success' ? '#badbcc' : type === 'warning' ? '#ffecb5' : type === 'info' ? '#b6effb' : '#f5c2c7';
        const toast = document.createElement('div');
        toast.setAttribute('role', 'alert');
        Object.assign(toast.style, {
            position: 'fixed',
            top: '70px',
            right: '10px',
            zIndex: '1050',
            maxWidth: '420px',
            background: bg,
            color: color,
            border: `1px solid ${border}`,
            borderRadius: '6px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
            padding: '12px 16px',
            fontSize: '14px',
            lineHeight: '1.4',
            transition: 'transform .4s ease, opacity .4s ease',
            transform: 'translateX(120%)',
            opacity: '0'
        });
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
    const suggestionBox = document.getElementById("addressSuggestions");
    // Pickup Form
    const branchSelect = document.getElementById("branchSelect");
    const pickupNameInput = document.getElementById("pickupNameInput");
    const pickupPhoneInput = document.getElementById("pickupPhoneInput");
    // Submit Button
    const btnSubmitOrder = document.getElementById("btnSubmitOrder");
    const orderNoteDeliveryInput = document.getElementById("orderNoteDelivery");
    const orderNotePickupInput = document.getElementById("orderNotePickup");
    const submitSpinner = btnSubmitOrder.querySelector('.spinner-border');

    // --- UTILITY ---
    // FIX: Chỉ định nghĩa hàm calcDistance MỘT LẦN.
    const calcDistance = (lat1, lon1, lat2, lon2) => {
        const R = 6371; // bán kính Trái Đất (km)
        const dLat = (lat2 - lat1) * Math.PI / 180;
        const dLon = (lon2 - lon1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) ** 2 +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
            Math.sin(dLon / 2) ** 2;
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    };

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

        setStep(isInfoComplete && selectedPayment ? 1 : 0);
    }

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
                showToast(`❌ ${result.errorMessage}`, 'danger');
            } else {
                hasPromoError = false;
                const totalDiscount = result.totalDiscount || 0;
                discountRow.classList.toggle('d-none', totalDiscount <= 0);
                discountAmountEl.textContent = `- ${totalDiscount.toLocaleString('vi-VN')} đ`;
                grandTotalEl.textContent = `${result.finalTotal.toLocaleString('vi-VN')} đ`;
                shippingFeeEl.textContent = `${result.finalShippingFee.toLocaleString('vi-VN')} đ`;

                if (Array.isArray(result.appliedMessages)) {
                    result.appliedMessages.forEach(m => { if (m) showToast(m, 'success'); });
                }
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
            branches = await res.json(); // Gán dữ liệu vào biến `branches` đã khai báo ở trên
            branchSelect.innerHTML = '<option value="">-- Chọn chi nhánh --</option>';
            branches.forEach(b => branchSelect.innerHTML += `<option value="${b.branchID}">${b.name} - ${b.address}</option>`);
        } catch (e) { console.error("Lỗi tải chi nhánh:", e); }
    }

    function updateShippingFee() {
        // --- BẮT ĐẦU KIỂM TRA ---
        console.log("Running updateShippingFee...");
        console.log("Current userPosition:", userPosition);
        console.log("Number of branches loaded:", branches.length);
        console.log("%cLOG 4: Entering updateShippingFee", "color: blue; font-weight: bold;");
        console.log("-> userPosition:", userPosition);
        console.log("-> branches.length:", branches.length);
        if (!userPosition || branches.length === 0) {
            currentShippingFee = 0;
            updateDiscountsAndRender(); // Cần gọi lại để cập nhật tổng tiền
            return;
        }

        let nearest = null;
        let minDist = Infinity;

        branches.forEach(b => {
            const dist = calcDistance(userPosition.lat, userPosition.lon, b.latitude, b.longitude);
            if (dist < minDist) {
                minDist = dist;
                nearest = b;
            }
        });

        console.log("%cLOG 5: Nearest branch found", "color: blue; font-weight: bold;");
        console.log("-> Nearest:", nearest);
        console.log("-> Min Distance:", minDist);

        if (!nearest) {
            nearestBranchInput.value = 'Không tìm thấy chi nhánh phù hợp.';
            return;
        };
        branchIdInput.value = nearest.branchID;
        nearestBranchInput.value = `${nearest.name} - ${minDist.toFixed(2)} km`;

        if (minDist > 15) {
            branchWarning.textContent = `Khoảng cách quá xa (>15km). Vui lòng chọn "Nhận tại cửa hàng".`;
            branchWarning.classList.remove('d-none');
            currentShippingFee = 0;
        } else {
            branchWarning.classList.add('d-none');
            currentShippingFee = Math.round(minDist * SHIPPING_RATE);
        }
        shippingFeeEl.setAttribute('data-value', currentShippingFee);
        updateDiscountsAndRender();
    }

    // FIX: Hợp nhất logic tìm chi nhánh gần nhất và cập nhật phí ship vào một hàm
    function findNearestBranchAndUpdate(lat, lon) {
        userPosition = { lat, lon };
        updateShippingFee();
    }

    // --- EVENT LISTENERS ---
    // FIX: Gom tất cả event listeners vào một khu vực và đảm bảo chỉ gán 1 lần.
    applyBtn.addEventListener("click", handleApplyCode);
    promoInput.addEventListener("keydown", e => e.key === "Enter" && (e.preventDefault(), handleApplyCode()));

    document.querySelectorAll('a[data-bs-toggle="tab"]').forEach(tab => {
        tab.addEventListener('shown.bs.tab', e => {
            isDeliveryMode = (e.target.id === 'delivery-tab-link');
            updateDiscountsAndRender();
            updateCheckoutStepUI();
        });
    });

    [nameInput, phoneInput, addressInput, pickupNameInput, pickupPhoneInput, branchSelect, detailAddressInput].forEach(input => {
        input?.addEventListener("input", updateCheckoutStepUI);
    });

    // Real-time phone validation
    const phoneRegex = /^(0[3|5|7|8|9])+([0-9]{8})\b/;
    
    function validatePhoneInput(input, errorElementId) {
        const phone = input.value.trim();
        const errorElement = document.getElementById(errorElementId);
        
        if (!phone) {
            if (errorElement) {
                errorElement.classList.add('d-none');
            }
            input.classList.remove('is-invalid');
            return;
        }
        
        if (!phoneRegex.test(phone)) {
            if (errorElement) {
                errorElement.textContent = phone.length < 10 
                    ? 'Số điện thoại phải có ít nhất 10 số.' 
                    : 'Số điện thoại không hợp lệ. Vui lòng nhập số bắt đầu bằng 0 và số thứ 2 là 3, 5, 7, 8 hoặc 9.';
                errorElement.classList.remove('d-none');
            }
            input.classList.add('is-invalid');
        } else {
            if (errorElement) {
                errorElement.classList.add('d-none');
            }
            input.classList.remove('is-invalid');
        }
    }

    // Validate phone on blur and input
    if (phoneInput) {
        phoneInput.addEventListener('blur', () => validatePhoneInput(phoneInput, 'phoneError'));
        phoneInput.addEventListener('input', () => {
            validatePhoneInput(phoneInput, 'phoneError');
            updateCheckoutStepUI();
        });
    }

    if (pickupPhoneInput) {
        pickupPhoneInput.addEventListener('blur', () => validatePhoneInput(pickupPhoneInput, 'pickupPhoneError'));
        pickupPhoneInput.addEventListener('input', () => {
            validatePhoneInput(pickupPhoneInput, 'pickupPhoneError');
            updateCheckoutStepUI();
        });
    }

    document.querySelectorAll('input[name="Payment"]').forEach(radio => {
        radio.addEventListener("change", updateCheckoutStepUI);
    });

    // Address Autocomplete
    let debounceTimer;
    addressInput.addEventListener("input", () => {
        const query = addressInput.value.trim();
        clearTimeout(debounceTimer);
        if (query.length < 3) {
            suggestionBox.style.display = "none";
            return;
        }
        debounceTimer = setTimeout(async () => {
            try {
                const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&countrycodes=vn&limit=5`);
                const data = await res.json();
                suggestionBox.innerHTML = "";
                if (data.length === 0) {
                    suggestionBox.style.display = "none";
                    return;
                }
                data.forEach(addr => {
                    const li = document.createElement("li");
                    li.className = "list-group-item list-group-item-action";
                    li.textContent = addr.display_name;
                    li.addEventListener("click", () => {
                        addressInput.value = addr.display_name;
                        suggestionBox.style.display = "none";
                        findNearestBranchAndUpdate(parseFloat(addr.lat), parseFloat(addr.lon));
                    });
                    suggestionBox.appendChild(li);
                });
                suggestionBox.style.display = "block";
            } catch (e) {
                console.error("Lỗi gợi ý địa chỉ:", e);
                suggestionBox.style.display = "none";
            }
        }, 150);
    });

    // "My Location" Button
    btnLocation.addEventListener("click", () => {
        if (!navigator.geolocation) return alert("Trình duyệt không hỗ trợ định vị.");
        addressInput.value = "Đang lấy vị trí...";
        navigator.geolocation.getCurrentPosition(async pos => {
            const { latitude: lat, longitude: lon } = pos.coords;
            try {
                const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`);
                const data = await res.json();
                addressInput.value = data?.display_name || "Không tìm thấy địa chỉ";
                findNearestBranchAndUpdate(lat, lon);
            } catch (err) {
                console.error(err);
                addressInput.value = "Lỗi khi lấy địa chỉ";
            }
        }, err => {
            alert(`❌ Không thể lấy vị trí: ${err.message}`);
            addressInput.value = ""; // Xoá chữ "Đang lấy vị trí..." nếu lỗi
        });
    });

    // Hide suggestions when clicking outside
    document.addEventListener("click", (e) => {
        if (!addressInput.contains(e.target) && !suggestionBox.contains(e.target)) {
            suggestionBox.style.display = "none";
        }
    });

    // Submit Order Button
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

        if (hasPromoError) {
            showToast("⚠️ Vui lòng xử lý lỗi mã khuyến mãi trước khi đặt hàng.", 'warning');
            return;
        }

        btnSubmitOrder.disabled = true;
        submitSpinner.classList.remove('d-none');

        const formData = new FormData(checkoutForm);
        formData.set("BranchID", branchIdValue);
        formData.append("PromoCode", appliedCodes.join(','));

        if (isDeliveryMode) {
            formData.set('Name', nameInput.value.trim());
            formData.set('Phone', phoneInput.value.trim());
            formData.set('Address', addressInput.value.trim());
            formData.set('DetailAddress', detailAddressInput.value.trim());
            formData.set('Note', (orderNoteDeliveryInput?.value || '').trim());
            formData.set('ShippingFee', String(currentShippingFee));
        } else {
            formData.set('Name', pickupNameInput.value.trim());
            formData.set('Phone', pickupPhoneInput.value.trim());
            const selectedBranchText = branchSelect.options[branchSelect.selectedIndex].text;
            formData.set('Address', `Nhận tại: ${selectedBranchText}`);
            formData.set('DetailAddress', '');
            formData.set('Note', (orderNotePickupInput?.value || '').trim());
            formData.set('ShippingFee', '0');
        }

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
                    window.location.href = `/Order/PayWithMomo`;
                } else {
                    window.location.href = `/Order/Confirmed/${result.orderId}`;
                }
            } else {
                showToast(`❌ Đặt hàng thất bại: ${result.message || 'Lỗi không xác định.'}`, 'danger');
            }
        } catch (error) {
            showToast('❌ Đã xảy ra lỗi kết nối. Vui lòng thử lại.', 'danger');
        } finally {
            btnSubmitOrder.disabled = false;
            submitSpinner.classList.add('d-none');
        }
    });

    // --- INITIALIZATION ---
    async function initializeCheckout() {
        console.log("%cLOG 1: Starting initializeCheckout", "color: green; font-weight: bold;");
        // Luôn chờ tải xong danh sách chi nhánh trước
        await loadBranches();

        const initialAddress = addressInput.value.trim();
        console.log("%cLOG 2: Checking for initial address", "color: green; font-weight: bold;");
        console.log("-> Address from input:", initialAddress);
        // KIỂM TRA: Nếu có địa chỉ được điền sẵn từ server
        if (initialAddress) {
            try {
                // Bước 1: "Dịch" địa chỉ chữ thành tọa độ số
                const encodedQuery = encodeURIComponent(initialAddress);
                const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodedQuery}&countrycodes=vn&limit=1`);
                const data = await res.json();
                console.log("%cLOG 3: API response received", "color: green; font-weight: bold;");
                console.log("-> API Data:", data);

                // Bước 2: Nếu dịch thành công, dùng tọa độ để tính toán
                if (data.length > 0) {
                    const addr = data[0];
                    // Hàm này sẽ tự động tính toán và cập nhật toàn bộ UI
                    findNearestBranchAndUpdate(parseFloat(addr.lat), parseFloat(addr.lon));
                } else {
                    // Nếu API không tìm thấy địa chỉ, vẫn phải cập nhật giá (với phí ship = 0)
                    console.warn("-> API found no matching address. Calling default updateDiscounts.");
                    updateDiscountsAndRender();
                }
            } catch (e) {
                console.error("-> ERROR during initial address fetch:", e);
                console.error("Lỗi tự động tìm địa chỉ ban đầu:", e);
                // Nếu có lỗi mạng, cũng phải cập nhật giá
                updateDiscountsAndRender();
            }
        } else {
            console.log("-> No initial address found. Calling default updateDiscounts.");
            // Nếu không có địa chỉ nào được điền sẵn, chỉ cần cập nhật giá mặc định
            updateDiscountsAndRender();
        }

        // Luôn cập nhật giao diện các bước ở cuối cùng
        updateCheckoutStepUI();
    }

    // Gọi hàm khởi tạo
    initializeCheckout();
});
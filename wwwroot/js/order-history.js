document.addEventListener('DOMContentLoaded', function () {
    const orderSummaries = document.querySelectorAll('.order-summary');
    const cancelForms = document.querySelectorAll('.cancel-order-form');
    const reorderButtons = document.querySelectorAll('.btn-reorder');
    let selectedCancelForm = null;

    orderSummaries.forEach(summary => {
        summary.addEventListener('click', function (event) {
            if (event.target.closest('button, a')) {
                return;
            }

            const orderCard = this.closest('.order-card');
            orderCard.classList.toggle('active');
        });
    });

    // Open modal for reason
    cancelForms.forEach(form => {
        const openBtn = form.querySelector('.btn-open-cancel');
        if (openBtn) {
            openBtn.addEventListener('click', function () {
                selectedCancelForm = form;
                const reasonInput = form.querySelector('.cancel-reason-input');
                const modalTextarea = document.getElementById('cancelReasonText');
                if (modalTextarea && reasonInput) {
                    modalTextarea.value = reasonInput.value || '';
                }

                const cancelModal = new bootstrap.Modal(document.getElementById('cancelModal'));
                cancelModal.show();
            });
        }
    });

    // Confirm cancel from modal
    const confirmBtn = document.getElementById('confirmCancelBtn');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', async function () {
            if (!selectedCancelForm) return;
            const reasonInput = selectedCancelForm.querySelector('.cancel-reason-input');
            const modalTextarea = document.getElementById('cancelReasonText');
            if (reasonInput && modalTextarea) {
                reasonInput.value = modalTextarea.value.trim();
            }

            const actionUrl = selectedCancelForm.getAttribute('action');
            const formData = new FormData(selectedCancelForm);

            try {
                const res = await fetch(actionUrl, {
                    method: 'POST',
                    body: formData,
                    credentials: 'same-origin'
                });
                const data = await res.json();
                if (data.success) {
                    const card = selectedCancelForm.closest('.order-card');
                    const statusBadge = card.querySelector('.status-badge');
                    statusBadge.textContent = 'Đã hủy';
                    statusBadge.className = 'status-badge status-đã-hủy';

                    const cancelBtn = selectedCancelForm.querySelector('.btn-open-cancel');
                    if (cancelBtn) {
                        cancelBtn.disabled = true;
                        cancelBtn.textContent = 'Đã hủy';
                    }

                    const modalEl = document.getElementById('cancelModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();
                    alert('Hủy đơn thành công');
                } else {
                    alert(data.message || 'Không thể hủy đơn');
                }
            } catch (err) {
                alert('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        });
    }

    // Reorder: copy items back to cart
    reorderButtons.forEach(btn => {
        btn.addEventListener('click', async function () {
            const orderId = this.getAttribute('data-order-id');
            if (!orderId) return;
            try {
                const formData = new FormData();
                formData.append('orderId', orderId);
                const res = await fetch('/Order/Reorder', {
                    method: 'POST',
                    body: formData,
                    credentials: 'same-origin'
                });
                const data = await res.json();
                if (data.success && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                } else {
                    alert(data.message || 'Không thể đặt lại.');
                }
            } catch (e) {
                alert('Có lỗi xảy ra. Vui lòng thử lại.');
            }
        });
    });
});
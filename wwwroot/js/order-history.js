document.addEventListener('DOMContentLoaded', function () {
    const orderSummaries = document.querySelectorAll('.order-summary');

    orderSummaries.forEach(summary => {
        summary.addEventListener('click', function (event) {
            if (event.target.closest('button, a')) {
                return;
            }

            const orderCard = this.closest('.order-card');
            orderCard.classList.toggle('active');
        });
    });
});
const cartItemsEl = document.getElementById("cart-items");
const cartTotalEl = document.getElementById("cart-total");
let cart = [];


// document.querySelectorAll(".btn-add").forEach(function (btn) {
//     btn.addEventListener("click", function () {
//         const productEl = this.closest(".product-item");
//         const name = productEl.querySelector("h3").textContent;
//         const activeSize = productEl.querySelector(".size-option.active");
//         if (!activeSize) {
//             alert("Vui lòng chọn size!");
//             return;
//         }
//         const size = activeSize.textContent;
//         const price = parseFloat(activeSize.getAttribute("data-price"));


//         const existing = cart.find(item => item.name === name && item.size === size);
//         if (existing) {
//             existing.qty++;
//         } else {
//             cart.push({ name, size, price, qty: 1 });
//         }

//         renderCart();
//     });
// });


// document.querySelectorAll(".size-options").forEach(group => {
//     const options = group.querySelectorAll(".size-option");
//     const priceEl = group.closest(".product-item").querySelector(".selected-price");

//     options.forEach(opt => {
//         opt.addEventListener("click", () => {
//             options.forEach(o => o.classList.remove("active"));
//             opt.classList.add("active");
//             const price = parseFloat(opt.getAttribute("data-price"));
//             priceEl.textContent = price.toLocaleString("vi-VN") + " đ";
//         });
//     });

//     if (options.length > 0) {
//         options[0].classList.add("active");
//     }
// });


document.querySelectorAll('.filter-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.filter-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');

        const categoryId = btn.getAttribute('data-category');

        document.querySelectorAll('.product-item').forEach(item => {
            if (categoryId === 'all' || item.dataset.category === categoryId) {
                item.style.display = 'flex';
            } else {
                item.style.display = 'none';
            }
        });
    });
});

document.addEventListener("DOMContentLoaded", function () {
    const searchInput = document.querySelector(".search-bar input");
    const products = document.querySelectorAll(".product-item");
    const noResult = document.querySelector(".no-result"); 

    function normalizeText(str) {
        return str
            .toLowerCase()
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d")
            .trim();
    }

    searchInput.addEventListener("input", function () {
        const keyword = normalizeText(this.value);
        let found = false;

        products.forEach(product => {
            const text = normalizeText(product.querySelector("h3").textContent);
            if (text.includes(keyword)) {
                product.style.display = "flex";
                found = true;
            } else {
                product.style.display = "none";
            }
        });

        noResult.style.display = found ? "none" : "flex";
    });
});




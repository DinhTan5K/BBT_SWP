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


const csrfToken = '@antiforgeryToken';
let currentCartData = [];

// chọn size hiển thị giá
document.querySelectorAll('.size-option').forEach(el => {
    el.addEventListener('click', () => {
        const parent = el.parentElement;
        parent.querySelectorAll('.size-option').forEach(s => s.classList.remove('active'));
        el.classList.add('active');

        const priceEl = parent.closest('.product-item').querySelector('.selected-price');
        const price = parseFloat(el.dataset.price);
        priceEl.textContent = price.toLocaleString('vi-VN') + ' đ';
    });
});

// mặc định chọn size đầu tiên
document.querySelectorAll('.size-options').forEach(group => {
    const options = group.querySelectorAll('.size-option');
    if (options.length > 0) {
        options[0].classList.add('active');
        const priceEl = group.closest('.product-item').querySelector('.selected-price');
        const defaultPrice = parseFloat(options[0].dataset.price);
        priceEl.textContent = defaultPrice.toLocaleString('vi-VN') + ' đ';
    }
});

// thêm vào giỏ
function addToCart(productId) {
    const productEl = document.querySelector(`.product-item[data-product-id='${productId}']`);
    if (!productEl) return;

    const selectedSizeEl = productEl.querySelector('.size-option.active') || productEl.querySelector('.size-option');
    const sizeId = parseInt(selectedSizeEl.dataset.sizeId);
    const unitPrice = parseFloat(selectedSizeEl.dataset.price);

    fetch('/Cart/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({
            productId: productId,
            productSizeId: sizeId,
            quantity: 1,
            price: unitPrice
        })
    })
        .then(res => res.json())
        .then(data => {

            if (data.redirectUrl) {
                window.location.href = data.redirectUrl;
                return;
            }

            if (data.success) loadCart();
            else alert('Lỗi: ' + data.message);
        })
        .catch(err => console.error(err));
}

// load giỏ hàng
function loadCart() {
    fetch('/Cart/Items')
        .then(res => res.json())
        .then(data => {
            currentCartData = data;
            const cartEl = document.getElementById('cart-items');
            const cartTotalEl = document.getElementById('cart-total');

            cartEl.innerHTML = "";
            let total = 0;

            if (data.length) {
                data.forEach((cd) => {
                    const div = document.createElement('div');
                    div.classList.add('cart-item');
                    div.innerHTML = `
                                <span class="cart-item-name">${cd.productName} - ${cd.size}</span>
                                <div class="cart-actions">
                                    <button class="qty-btn" data-cart-detail-id="${cd.cartDetailID}" data-action="dec">-</button>
                                    <span class="qty">${cd.quantity}</span>
                                    <button class="qty-btn" data-cart-detail-id="${cd.cartDetailID}" data-action="inc">+</button>
                                </div>
                            `;
                    cartEl.appendChild(div);
                    total += cd.total;
                });
            } else {
                cartEl.innerHTML = "<p>Chưa có sản phẩm nào.</p>";
            }

            cartTotalEl.textContent = total.toLocaleString() + " đ";
        });
}

// cập nhật số lượng
document.getElementById('cart-items').addEventListener('click', function (e) {
    if (!e.target.classList.contains('qty-btn')) return;

    const cartDetailId = e.target.dataset.cartDetailId;
    const action = e.target.dataset.action;
    const qtyEl = e.target.parentElement.querySelector('.qty');
    let newQty = parseInt(qtyEl.textContent);

    if (action === 'inc') newQty++;
    else if (action === 'dec') newQty--;

    if (newQty < 0) newQty = 0;

    fetch('/Cart/Update', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({ cartDetailId, quantity: newQty })
    }).then(() => loadCart());
});

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.btn-add').forEach(btn => {
        btn.addEventListener('click', () => {
            const productEl = btn.closest('.product-item');
            const productId = parseInt(productEl.dataset.productId);
            addToCart(productId);
        });
    });

    loadCart();
});


// AJAX-based Product Filtering, Sorting, Pagination
let currentFilters = {
    categoryId: null,
    searchTerm: '',
    minPrice: null,
    maxPrice: null,
    sortBy: '',
    wishlistOnly: false,
    page: 1,
    pageSize: 6
};

let isLoading = false;
let currentRequestId = 0; // Track request để tránh race condition

// Load products via AJAX
function loadProducts() {
    if (isLoading) return;
    isLoading = true;
    currentRequestId++; // Tăng ID cho request mới
    const thisRequestId = currentRequestId; // Lưu ID của request hiện tại

    const grid = document.getElementById('productsGrid');
    const loadingIndicator = document.getElementById('loadingIndicator');
    const noResult = document.querySelector('.no-result');
    
    // Show loading
    if (grid) grid.style.display = 'none';
    if (loadingIndicator) loadingIndicator.style.display = 'flex';
    if (noResult) noResult.style.display = 'none';

    const params = new URLSearchParams();
    if (currentFilters.categoryId) params.append('CategoryId', currentFilters.categoryId);
    if (currentFilters.searchTerm) params.append('SearchTerm', currentFilters.searchTerm);
    if (currentFilters.minPrice !== null) params.append('MinPrice', currentFilters.minPrice);
    if (currentFilters.maxPrice !== null) params.append('MaxPrice', currentFilters.maxPrice);
    if (currentFilters.sortBy) params.append('SortBy', currentFilters.sortBy);
    params.append('WishlistOnly', currentFilters.wishlistOnly.toString());
    params.append('Page', currentFilters.page);
    params.append('PageSize', currentFilters.pageSize);

    fetch(`/Product/FilterProducts?${params}`)
        .then(res => res.json())
        .then(data => {
            // Chỉ xử lý nếu đây là request mới nhất (tránh race condition)
            if (thisRequestId !== currentRequestId) {
                isLoading = false;
                return; // Bỏ qua response của request cũ
            }
            
            // Đảm bảo ẩn no-result và loading trước khi render
            const noResult = document.querySelector('.no-result');
            if (noResult) noResult.style.display = 'none';
            if (loadingIndicator) loadingIndicator.style.display = 'none';
            
            // Set isLoading = false TRƯỚC khi render để renderProducts() có thể check
            // Nhưng chỉ khi đây là request mới nhất (đã check ở trên)
            isLoading = false;
            
            // Render products (hàm này sẽ tự quản lý display của grid)
            renderProducts(data.products, thisRequestId);
            renderPagination(data);
            // Không cần set grid.display ở đây nữa vì renderProducts() đã xử lý
        })
        .catch(err => {
            // Chỉ xử lý lỗi nếu đây là request mới nhất
            if (thisRequestId !== currentRequestId) {
                isLoading = false;
                return;
            }
            
            console.error('Error loading products:', err);
            const noResult = document.querySelector('.no-result');
            if (noResult) noResult.style.display = 'none';
            
            if (grid) {
                grid.innerHTML = '<p style="grid-column: 1/-1; text-align: center; padding: 2rem; color: var(--text-secondary);">Có lỗi xảy ra khi tải sản phẩm. Vui lòng thử lại!</p>';
                grid.style.display = 'grid';
            }
            if (loadingIndicator) loadingIndicator.style.display = 'none';
            isLoading = false;
        });
}

// Render products from JSON
function renderProducts(products, requestId = null) {
    const grid = document.getElementById('productsGrid');
    const noResult = document.querySelector('.no-result');
    
    // Đảm bảo ẩn no-result trước khi kiểm tra
    if (noResult) noResult.style.display = 'none';
    
    // Nếu có requestId, chỉ xử lý nếu đây là request mới nhất
    if (requestId !== null && requestId !== currentRequestId) {
        return; // Bỏ qua nếu không phải request mới nhất
    }
    
    if (!products || products.length === 0) {
        // Đảm bảo grid ẩn và rỗng trước khi hiện no-result
        if (grid) {
            grid.innerHTML = '';
            grid.style.display = 'none';
        }
        
        // CHỈ hiển thị no-result nếu:
        // 1. Không đang loading
        // 2. Đây là request mới nhất (hoặc không có requestId tracking)
        const isLatestRequest = requestId === null || requestId === currentRequestId;
        if (noResult && !isLoading && isLatestRequest) {
            // Dùng requestAnimationFrame để đảm bảo DOM đã cập nhật
            requestAnimationFrame(() => {
                // Triple check: không loading VÀ là request mới nhất
                if (!isLoading && (requestId === null || requestId === currentRequestId)) {
                    noResult.style.display = 'flex';
                }
            });
        }
        if (grid) grid.classList.remove('few-items', 'single-item');
        return;
    }
    
    // Đảm bảo grid được hiển thị khi có products
    if (grid) {
        grid.style.display = 'grid';
    }
    
    // Add class based on item count for better layout
    grid.classList.remove('few-items', 'single-item');
    if (products.length === 1) {
        grid.classList.add('single-item');
    } else if (products.length <= 3) {
        grid.classList.add('few-items');
    }
    
    grid.innerHTML = products.map(p => {
        const minPrice = p.minPrice || (p.productSizes && p.productSizes.length > 0 ? Math.min(...p.productSizes.map(ps => ps.price)) : 0);
        const heartClass = p.isWishlisted ? 'text-danger' : 'text-secondary';
        const heartTitle = p.isWishlisted ? 'Bỏ khỏi yêu thích' : 'Thêm vào yêu thích';
        
        const sizesHtml = p.productSizes ? p.productSizes.map(ps => 
            `<div class="size-option" data-size-id="${ps.productSizeID}" data-price="${ps.price}">${ps.size}</div>`
        ).join('') : '';
        
        return `
            <div class="product-item position-relative" data-category="${p.categoryID}" data-product-id="${p.productID}" data-name="${p.productName.toLowerCase()}">
                <div class="product-media">
                    <button type="button" class="btn-wishlist" data-id="${p.productID}" data-bs-toggle="tooltip" title="${heartTitle}">
                        <i class="fa fa-heart heart-icon ${heartClass}"></i>
                    </button>
                    <img src="${p.image_Url || ''}" alt="${p.productName}" />
                    <button type="button" class="quickview-overlay btn-quickview" data-id="${p.productID}" aria-label="Xem nhanh">
                        <i class="fa fa-eye"></i> Xem nhanh
                    </button>
                </div>
                <div class="product-info-wrapper">
                    <h3><a href="/Product/Detail/${p.productID}" style="text-decoration: none; color: inherit;">${p.productName}</a></h3>
                    <p class="price">Giá:&nbsp<span class="selected-price">${minPrice.toLocaleString('vi-VN')} đ</span></p>
                    <div class="product-actions">
                        <div class="size-options" data-product-id="${p.productID}">
                            ${sizesHtml}
                        </div>
                        <button class="btn-add">Add to Cart</button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
    
    // Re-initialize size options and wishlist
    initializeProductInteractions();
}

// Render pagination
function renderPagination(data) {
    const container = document.getElementById('pagination-container');
    if (!container) return;
    
    if (data.totalPages <= 1) {
        container.innerHTML = '';
        container.style.display = 'none';
        return;
    }
    
    container.style.display = 'flex';
    let html = '';
    
    // Previous button
    html += `<button class="page-btn" ${data.currentPage === 1 ? 'disabled' : ''} data-page="${data.currentPage - 1}">‹</button>`;
    
    // Page numbers
    for (let i = 1; i <= data.totalPages; i++) {
        if (i === 1 || i === data.totalPages || (i >= data.currentPage - 1 && i <= data.currentPage + 1)) {
            html += `<button class="page-btn ${i === data.currentPage ? 'active' : ''}" data-page="${i}">${i}</button>`;
        } else if (i === data.currentPage - 2 || i === data.currentPage + 2) {
            html += `<span class="page-dots">...</span>`;
        }
    }
    
    // Next button
    html += `<button class="page-btn" ${data.currentPage === data.totalPages ? 'disabled' : ''} data-page="${data.currentPage + 1}">›</button>`;
    
    container.innerHTML = html;
    
    // Bind pagination clicks
    container.querySelectorAll('.page-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const page = parseInt(btn.dataset.page);
            if (page && page !== data.currentPage && page >= 1 && page <= data.totalPages) {
                currentFilters.page = page;
                loadProducts();
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        });
    });
}

// Initialize product interactions (size selection, add to cart, wishlist, quickview)
function initializeProductInteractions() {
    // Size options
    document.querySelectorAll('.size-options').forEach(group => {
        const options = group.querySelectorAll('.size-option');
        options.forEach((opt, idx) => {
            opt.addEventListener('click', () => {
                options.forEach(o => o.classList.remove('active'));
                opt.classList.add('active');
                const priceEl = group.closest('.product-item').querySelector('.selected-price');
                if (priceEl) {
                    const price = parseFloat(opt.dataset.price);
                    priceEl.textContent = price.toLocaleString('vi-VN') + ' đ';
                }
            });
            if (idx === 0) opt.classList.add('active');
        });
    });
    
    // Add to cart buttons
    document.querySelectorAll('.btn-add').forEach(btn => {
        btn.addEventListener('click', () => {
            const productEl = btn.closest('.product-item');
            const productId = parseInt(productEl.dataset.productId);
            addToCart(productId);
        });
    });
    
    // Wishlist buttons (rebind)
    document.querySelectorAll('.btn-wishlist').forEach(btn => {
        // Remove old listeners by cloning
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        newBtn.addEventListener('click', handleWishlistClick);
    });
    
    // Quickview buttons (rebind) - Redirect to Detail page
    document.querySelectorAll('.btn-quickview').forEach(btn => {
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        newBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            const productId = this.dataset.id;
            if (productId) {
                window.location.href = `/Product/Detail/${productId}`;
            }
        });
    });
}

function handleWishlistClick() {
    const productId = this.dataset.id;
    const icon = this.querySelector('.heart-icon');
    
    if (!productId) {
        console.error('ProductId is missing');
        return;
    }
    
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: { 
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({ productId: parseInt(productId) })
    })
    .then(res => {
        if (!res.ok) {
            throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
    })
    .then(data => {
        if (data && data.success === true) {
            setWishlistStateFor(productId, data.isWishlisted);
        } else if (data && data.error) {
            console.error('Wishlist error:', data.error);
            alert(data.error);
        }
    })
    .catch(error => {
        console.error('Wishlist toggle error:', error);
        alert('Có lỗi xảy ra khi thao tác với wishlist. Vui lòng thử lại.');
    });
}

// Add to cart from modal
function addToCartFromModal(productId) {
    const modalRoot = document.getElementById('quickViewContent');
    let productEl = null;
    if (modalRoot) {
        productEl = modalRoot.querySelector(`.product-item[data-product-id='${productId}']`);
    }
    if (!productEl) {
        productEl = document.querySelector(`.product-item[data-product-id='${productId}']`);
    }
    if (!productEl) return;

    const selectedSizeEl = productEl.querySelector('.size-option.active') || productEl.querySelector('.size-option');
    if (!selectedSizeEl) { 
        alert('Vui lòng chọn size'); 
        return; 
    }

    const sizeId = parseInt(selectedSizeEl.dataset.sizeId);
    const unitPrice = parseFloat(selectedSizeEl.dataset.price);
    
    // Get csrfToken from window or use empty string
    const csrfToken = window.csrfToken || '';

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
        if (data.success) { 
            if (typeof loadCart === 'function') loadCart();
            if (typeof showCartToast === 'function') showCartToast('Đã thêm sản phẩm vào giỏ hàng!');
        }
        else alert('Lỗi: ' + (data.message || 'Không thể thêm vào giỏ'));
    })
    .catch(err => console.error(err));
}

// Make loadProducts globally accessible
window.loadProducts = loadProducts;
window.currentFilters = currentFilters;

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initFilters);
} else {
    initFilters();
}

// Load category product counts and update filter buttons
function loadCategoryCounts() {
    fetch('/Product/GetCategoryCounts')
        .then(res => res.json())
        .then(counts => {
            // Update each category filter button with product count (skip wishlist buttons)
            document.querySelectorAll('.filter-btn[data-category]').forEach(btn => {
                const categoryId = btn.dataset.category;
                
                // Remove existing count badge if any
                const existingBadge = btn.querySelector('.category-count-badge');
                if (existingBadge) {
                    existingBadge.remove();
                }
                
                let count = 0;
                if (categoryId === 'all') {
                    // For "Tất cả", sum all counts
                    count = Object.values(counts).reduce((sum, count) => sum + count, 0);
                } else {
                    const catId = parseInt(categoryId);
                    count = counts[catId] || 0;
                }
                
                // Get base text (remove any existing count)
                let baseText = btn.textContent.trim();
                baseText = baseText.replace(/\s*\(\d+\)\s*$/, '').trim();
                
                // Add count badge if count > 0
                if (count > 0) {
                    btn.innerHTML = `${baseText} <span class="category-count-badge">${count}</span>`;
                } else {
                    btn.textContent = baseText;
                }
            });
        })
        .catch(err => console.error('Error loading category counts:', err));
}

function initFilters() {
    // Load category counts first
    loadCategoryCounts();
    
    // Wishlist filter (toggle on/off)
    const wishlistButtons = document.querySelectorAll('.filter-wishlist');
    console.log('Found wishlist buttons:', wishlistButtons.length);
    wishlistButtons.forEach(btn => {
        // Remove any existing listeners by cloning
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        
        newBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            console.log('Wishlist button clicked!');
            const isActive = this.classList.contains('active');
            console.log('Current active state:', isActive);
            
            if (isActive) {
                this.classList.remove('active');
                currentFilters.wishlistOnly = false;
                console.log('Removed active, wishlistOnly = false');
            } else {
                this.classList.add('active');
                currentFilters.wishlistOnly = true;
                console.log('Added active, wishlistOnly = true');
            }
            
            // Force reflow to ensure CSS applies
            void this.offsetWidth;
            
            console.log('Current filters:', currentFilters);
            currentFilters.page = 1;
            loadProducts();
        });
    });
    
    // Category filter
    document.querySelectorAll('.filter-btn[data-category]').forEach(btn => {
        btn.addEventListener('click', () => {
            // Only remove active from category buttons, not wishlist buttons
            document.querySelectorAll('.filter-btn[data-category]').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const catId = btn.dataset.category === 'all' ? null : parseInt(btn.dataset.category);
            currentFilters.categoryId = catId;
            currentFilters.page = 1;
            loadProducts();
        });
    });
    
    // Search (debounced) - tăng thời gian debounce để giảm số request
    let searchTimeout;
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            // Ẩn no-result ngay khi bắt đầu gõ (trước cả khi debounce)
            const noResult = document.querySelector('.no-result');
            if (noResult) noResult.style.display = 'none';
            
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                currentFilters.searchTerm = e.target.value.trim();
                currentFilters.page = 1;
                loadProducts();
            }, 400); // Tăng từ 300ms lên 400ms
        });
    }
    
    // Custom Sort dropdown
    const customSelect = document.querySelector('.custom-select');
    const sortTrigger = document.getElementById('sortTrigger');
    const sortOptions = document.getElementById('sortOptions');
    const sortSelect = document.getElementById('sortSelect');
    
    if (customSelect && sortTrigger && sortOptions) {
        // Toggle dropdown
        sortTrigger.addEventListener('click', (e) => {
            e.stopPropagation();
            customSelect.classList.toggle('active');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!customSelect.contains(e.target)) {
                customSelect.classList.remove('active');
            }
        });
        
        // Handle option click
        sortOptions.querySelectorAll('.custom-select-option').forEach(option => {
            option.addEventListener('click', function() {
                const value = this.dataset.value;
                const text = this.textContent;
                
                // Update trigger text
                sortTrigger.querySelector('span').textContent = text;
                
                // Update hidden select (for compatibility)
                if (sortSelect) {
                    sortSelect.value = value;
                }
                
                // Update selected state
                sortOptions.querySelectorAll('.custom-select-option').forEach(opt => {
                    opt.classList.remove('selected');
                    opt.removeAttribute('data-selected');
                });
                this.classList.add('selected');
                this.setAttribute('data-selected', 'true');
                
                // Close dropdown
                customSelect.classList.remove('active');
                
                // Trigger sort
                currentFilters.sortBy = value;
                currentFilters.page = 1;
                loadProducts();
            });
        });
        
        // Set initial selected state
        const initialSelected = sortOptions.querySelector('[data-selected="true"]');
        if (initialSelected) {
            sortTrigger.querySelector('span').textContent = initialSelected.textContent;
        }
    }
    
    // Fallback for old select (if exists and visible)
    if (sortSelect && sortSelect.offsetParent !== null) {
        sortSelect.addEventListener('change', (e) => {
            currentFilters.sortBy = e.target.value;
            currentFilters.page = 1;
            loadProducts();
        });
    }
    
    // Initial load
    loadProducts();
}


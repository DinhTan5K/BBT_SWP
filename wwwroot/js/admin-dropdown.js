// Custom Dropdown Component cho Admin Panel
// Sử dụng để thay thế select elements với dropdown đẹp hơn

document.addEventListener('DOMContentLoaded', function() {
    // Tìm tất cả select trong admin panel để convert sang custom dropdown
    // Ưu tiên: .admin-filter-select select, sau đó là tất cả select trong main
    const selects = document.querySelectorAll('.admin-filter-select select, main select, .card select');
    
    selects.forEach(select => {
        // Bỏ qua nếu đã được convert
        if (select.closest('.admin-custom-dropdown')) {
            return;
        }
        
        // Bỏ qua nếu select đang trong modal và có class form-select (có thể là Bootstrap select)
        // Hoặc nếu có data attribute để skip
        if (select.hasAttribute('data-skip-custom-dropdown') || 
            select.classList.contains('form-select') && select.closest('.modal')) {
            return;
        }
        
        // Tạo wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'admin-custom-dropdown';
        
        // Tạo trigger
        const trigger = document.createElement('div');
        trigger.className = 'admin-custom-dropdown-trigger';
        trigger.textContent = select.options[select.selectedIndex]?.text || 'Chọn...';
        
        // Tạo options container
        const optionsContainer = document.createElement('div');
        optionsContainer.className = 'admin-custom-dropdown-options';
        
        // Tạo options
        Array.from(select.options).forEach((option, index) => {
            const optionDiv = document.createElement('div');
            optionDiv.className = 'admin-custom-dropdown-option';
            if (option.selected) {
                optionDiv.classList.add('selected');
            }
            optionDiv.textContent = option.text;
            optionDiv.dataset.value = option.value;
            
            optionDiv.addEventListener('click', function() {
                // Update select
                select.value = option.value;
                select.dispatchEvent(new Event('change', { bubbles: true }));
                
                // Update trigger
                trigger.textContent = option.text;
                
                // Update selected state
                optionsContainer.querySelectorAll('.admin-custom-dropdown-option').forEach(opt => {
                    opt.classList.remove('selected');
                });
                optionDiv.classList.add('selected');
                
                // Close dropdown
                wrapper.classList.remove('active');
            });
            
            optionsContainer.appendChild(optionDiv);
        });
        
        // Wrap select
        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(trigger);
        wrapper.appendChild(optionsContainer);
        wrapper.appendChild(select);
        select.style.display = 'none';
        
        // Toggle dropdown
        trigger.addEventListener('click', function(e) {
            e.stopPropagation();
            const isActive = wrapper.classList.contains('active');
            
            // Close all other dropdowns
            document.querySelectorAll('.admin-custom-dropdown.active').forEach(dd => {
                if (dd !== wrapper) {
                    dd.classList.remove('active');
                    // Remove dropdown-active class from parent cards
                    const parentCard = dd.closest('.card');
                    if (parentCard) {
                        parentCard.classList.remove('dropdown-active');
                    }
                }
            });
            
            wrapper.classList.toggle('active', !isActive);
            
            // Toggle dropdown-active class on parent card để fix overflow
            const parentCard = wrapper.closest('.card');
            if (parentCard) {
                if (!isActive) {
                    parentCard.classList.add('dropdown-active');
                } else {
                    // Delay để dropdown close animation hoàn thành
                    setTimeout(() => {
                        parentCard.classList.remove('dropdown-active');
                    }, 300);
                }
            }
        });
    });
    
    // Close dropdown khi click outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.admin-custom-dropdown')) {
            document.querySelectorAll('.admin-custom-dropdown.active').forEach(dd => {
                dd.classList.remove('active');
                // Remove dropdown-active class from parent cards
                const parentCard = dd.closest('.card');
                if (parentCard) {
                    setTimeout(() => {
                        parentCard.classList.remove('dropdown-active');
                    }, 300);
                }
            });
        }
    });
    
    // Close dropdown khi press Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.admin-custom-dropdown.active').forEach(dd => {
                dd.classList.remove('active');
                // Remove dropdown-active class from parent cards
                const parentCard = dd.closest('.card');
                if (parentCard) {
                    setTimeout(() => {
                        parentCard.classList.remove('dropdown-active');
                    }, 300);
                }
            });
        }
    });
});


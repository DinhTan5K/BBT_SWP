// privacy.js - Script cho trang Chính sách bảo mật

// Scroll Progress Bar
function updateProgressBar() {
    const winScroll = document.body.scrollTop || document.documentElement.scrollTop;
    const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    const scrolled = (height > 0) ? (winScroll / height) * 100 : 0;
    const bar = document.getElementById('progressBar');
    if (bar) bar.style.width = scrolled + '%';
}
window.addEventListener('scroll', updateProgressBar);
window.addEventListener('resize', updateProgressBar);

// Smooth Scroll cho liên kết điều hướng
document.addEventListener('click', function(e) {
    const el = e.target.closest('.nav-link');
    if (el) {
        e.preventDefault();
        const targetId = el.getAttribute('href');
        const targetSection = document.querySelector(targetId);
        if (targetSection) {
            targetSection.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }
}, { passive: true });

// Cập nhật link active khi cuộn
function updateActiveNavLink() {
    const sections = document.querySelectorAll('.section');
    const navLinks = document.querySelectorAll('.nav-link');
    let currentSection = '';

    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionHeight = section.clientHeight;
        if (window.pageYOffset >= sectionTop - 200) {
            currentSection = section.getAttribute('id');
        }
    });

    navLinks.forEach(link => {
        link.classList.remove('active');
        if (link.getAttribute('href') === '#' + currentSection) {
            link.classList.add('active');
        }
    });
}
window.addEventListener('scroll', updateActiveNavLink);
window.addEventListener('resize', updateActiveNavLink);

// Hiệu ứng reveal cho section khi cuộn
function revealSections() {
    document.querySelectorAll('.section').forEach(section => {
        const sectionTop = section.getBoundingClientRect().top;
        if (sectionTop < window.innerHeight - 100) {
            section.classList.add('visible');
        }
    });
}
window.addEventListener('scroll', revealSections);
window.addEventListener('load', revealSections);

// Accordion functionality
document.addEventListener('click', function(e) {
    const header = e.target.closest('.accordion-header');
    if (!header) return;

    const accordionItem = header.parentElement;
    const isActive = accordionItem.classList.contains('active');

    // Đóng tất cả
    document.querySelectorAll('.accordion-item').forEach(item => item.classList.remove('active'));

    // Mở nếu chưa active
    if (!isActive) {
        accordionItem.classList.add('active');
    }
}, { passive: true });

// Khởi tạo khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', function() {
    updateProgressBar();
    updateActiveNavLink();
    revealSections();
});
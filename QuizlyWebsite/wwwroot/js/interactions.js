// ===== QUIZ INTERACTIONS =====
document.addEventListener('DOMContentLoaded', function () {
    // Smooth scroll for anchors
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });

    // Add loading state to buttons
    document.querySelectorAll('button[type="submit"]').forEach(btn => {
        btn.addEventListener('click', function () {
            if (this.classList.contains('loading')) return;
            this.classList.add('opacity-50', 'cursor-not-allowed');
        });
    });

    // Toast notifications
    const showToast = (message, type = 'info') => {
        const toast = document.createElement('div');
        toast.className = `fixed bottom-4 right-4 px-6 py-3 rounded-lg text-white font-semibold animate-slideDown ${
            type === 'success' ? 'bg-green-500' :
            type === 'error' ? 'bg-red-500' :
            'bg-blue-500'
        }`;
        toast.textContent = message;
        document.body.appendChild(toast);

        setTimeout(() => {
            toast.remove();
        }, 3000);
    };

    window.showToast = showToast;

    // Quiz timer with visual feedback
    const initTimer = (duration) => {
        let timeLeft = duration * 60;
        return setInterval(() => {
            timeLeft--;
            if (timeLeft <= 0) {
                showToast('Hết giờ! Bài thi sẽ được nộp tự động', 'warning');
            }
        }, 1000);
    };

    window.initTimer = initTimer;

    // Question validation
    const validateAnswer = (selectedOption) => {
        if (!selectedOption) {
            showToast('Vui lòng chọn một đáp án', 'error');
            return false;
        }
        return true;
    };

    window.validateAnswer = validateAnswer;
});

// ===== FORM VALIDATION =====
const validateForm = (formId) => {
    const form = document.getElementById(formId);
    if (!form) return false;

    const inputs = form.querySelectorAll('input[required], textarea[required], select[required]');
    let isValid = true;

    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.classList.add('border-red-500');
            isValid = false;
        } else {
            input.classList.remove('border-red-500');
        }
    });

    return isValid;
};

window.validateForm = validateForm;

// ===== FILTER & SEARCH =====
const initializeFilters = () => {
    const filters = document.querySelectorAll('[data-filter]');
    const items = document.querySelectorAll('[data-item]');

    filters.forEach(filter => {
        filter.addEventListener('change', () => {
            const activeFilters = Array.from(filters)
                .filter(f => f.checked)
                .map(f => f.value);

            items.forEach(item => {
                const itemCategories = item.dataset.item.split(',');
                const shouldShow = activeFilters.length === 0 || 
                    itemCategories.some(cat => activeFilters.includes(cat));
                item.style.display = shouldShow ? '' : 'none';
            });
        });
    });
};

// ===== PAGINATION =====
const setupPagination = (totalPages, currentPage, onPageChange) => {
    const paginationBtns = document.querySelectorAll('[data-page]');
    paginationBtns.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const page = parseInt(btn.dataset.page);
            if (onPageChange) onPageChange(page);
        });
    });
};

// ===== DARK MODE TOGGLE =====
const toggleDarkMode = () => {
    const html = document.documentElement;
    const isDark = html.classList.toggle('dark');
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
};

// Initialize dark mode from localStorage
window.addEventListener('load', () => {
    const theme = localStorage.getItem('theme') || 'light';
    if (theme === 'dark') {
        document.documentElement.classList.add('dark');
    }
});

window.toggleDarkMode = toggleDarkMode;

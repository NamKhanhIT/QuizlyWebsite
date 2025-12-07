(function () {
    'use strict';

    const navLinks = document.querySelectorAll('.nav-link');
    const contentTabs = document.querySelectorAll('.content-tab');

    function switchTab(tabId) {
        // Hide all tabs
        contentTabs.forEach(tab => {
            tab.style.display = 'none';
        });

        // Remove active class from all nav links
        navLinks.forEach(link => {
            link.classList.remove('active');
        });

        // Show selected tab
        const selectedTab = document.getElementById(tabId);
        if (selectedTab) {
            selectedTab.style.display = 'block';

            // Trigger animations for the active tab
            triggerTabAnimations(tabId);
        }

        // Add active class to selected nav link
        const selectedNav = document.querySelector(`[data-tab="${tabId}"]`);
        if (selectedNav) {
            selectedNav.classList.add('active');
        }

        // Save to localStorage
        localStorage.setItem('currentTab', tabId);

        // Update URL hash
        history.replaceState(null, null, `#${tabId}`);

        // Scroll to top smoothly
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Add click events to nav links
    navLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const tabId = this.getAttribute('data-tab');
            switchTab(tabId);
        });
    });

    // Initialize on page load
    window.addEventListener('DOMContentLoaded', function () {
        let initialTab = 'courses';

        // Check URL hash
        const hash = window.location.hash.substring(1);
        if (hash && document.getElementById(hash)) {
            initialTab = hash;
        } else {
            // Check localStorage
            const savedTab = localStorage.getItem('currentTab');
            if (savedTab && document.getElementById(savedTab)) {
                initialTab = savedTab;
            }
        }

        switchTab(initialTab);
    });

    const mobileMenuBtn = document.getElementById('mobileMenuBtn');
    const sidebar = document.querySelector('.sidebar');

    if (mobileMenuBtn && sidebar) {
        mobileMenuBtn.addEventListener('click', function () {
            sidebar.classList.toggle('active');
        });

        // Close sidebar when clicking nav link on mobile
        navLinks.forEach(link => {
            link.addEventListener('click', function () {
                if (window.innerWidth <= 768) {
                    sidebar.classList.remove('active');
                }
            });
        });

        // Close sidebar when clicking outside on mobile
        document.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) {
                if (!sidebar.contains(e.target) && !mobileMenuBtn.contains(e.target)) {
                    sidebar.classList.remove('active');
                }
            }
        });
    }

    const avatarUploadInput = document.getElementById('avatarUploadInput');
    const avatarPreviewImg = document.getElementById('avatarPreviewImg');

    if (avatarUploadInput && avatarPreviewImg) {
        avatarUploadInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file && file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (event) {
                    if (avatarPreviewImg.tagName === 'IMG') {
                        avatarPreviewImg.src = event.target.result;
                    } else {
                        // Replace placeholder with image
                        const img = document.createElement('img');
                        img.src = event.target.result;
                        img.alt = 'Avatar';
                        img.id = 'avatarPreviewImg';
                        avatarPreviewImg.parentNode.replaceChild(img, avatarPreviewImg);
                    }
                };
                reader.readAsDataURL(file);
            }
        });
    }

    function animateProgressBars() {
        const progressBars = document.querySelectorAll('.xp-bar-fill, .xp-fill-modern, .mini-progress-fill');

        progressBars.forEach(bar => {
            const targetWidth = bar.style.width;
            bar.style.width = '0%';

            setTimeout(() => {
                bar.style.transition = 'width 1s cubic-bezier(0.4, 0, 0.2, 1)';
                bar.style.width = targetWidth;
            }, 100);
        });
    }

    function animateCircularProgress() {
        const circles = document.querySelectorAll('.circle-progress');

        circles.forEach(circle => {
            const targetOffset = circle.style.strokeDashoffset;
            circle.style.strokeDashoffset = '339.292';

            setTimeout(() => {
                circle.style.transition = 'stroke-dashoffset 1.5s cubic-bezier(0.4, 0, 0.2, 1)';
                circle.style.strokeDashoffset = targetOffset;
            }, 200);
        });
    }

    function triggerTabAnimations(tabId) {
        switch (tabId) {
            case 'progress':
                setTimeout(() => {
                    animateProgressBars();
                    animateCircularProgress();
                }, 100);
                break;
            case 'examresults':
                setTimeout(animateProgressBars, 100);
                break;
            case 'statistics':
                setTimeout(animateStatCards, 100);
                break;
        }
    }

    function animateStatCards() {
        const statCards = document.querySelectorAll('.stat-card-modern');

        statCards.forEach((card, index) => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';

            setTimeout(() => {
                card.style.transition = 'all 0.5s cubic-bezier(0.4, 0, 0.2, 1)';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, index * 80);
        });
    }

    const courseCards = document.querySelectorAll('.course-card-modern');
    courseCards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-6px)';
        });
        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });

    const examCards = document.querySelectorAll('.exam-card-modern');
    examCards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-6px)';
        });
        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });


    window.deleteCourse = function (courseId) {
        if (confirm('Bạn có chắc chắn muốn xóa khóa học này? Hành động này không thể hoàn tác.')) {
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = `/Course/Delete/${courseId}`;

            const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]');
            if (csrfToken) {
                const tokenInput = document.createElement('input');
                tokenInput.type = 'hidden';
                tokenInput.name = '__RequestVerificationToken';
                tokenInput.value = csrfToken.value;
                form.appendChild(tokenInput);
            }

            document.body.appendChild(form);
            form.submit();
        }
    };


    const passwordForm = document.querySelector('form[asp-action="ChangePassword"]');

    if (passwordForm) {
        passwordForm.addEventListener('submit', function (e) {
            const newPassword = document.getElementById('newPasswordInput').value;
            const confirmPassword = document.getElementById('confirmPasswordInput').value;

            if (newPassword !== confirmPassword) {
                e.preventDefault();
                showNotification('Mật khẩu mới và xác nhận mật khẩu không khớp!', 'error');
                return false;
            }

            if (newPassword.length < 6) {
                e.preventDefault();
                showNotification('Mật khẩu phải có ít nhất 6 ký tự!', 'error');
                return false;
            }
        });
    }

    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i>
                <span>${message}</span>
            </div>
        `;

        notification.style.cssText = `
            position: fixed;
            top: 90px;
            right: 24px;
            background: ${type === 'success' ? 'var(--secondary)' : type === 'error' ? 'var(--danger)' : 'var(--info)'};
            color: white;
            padding: 16px 24px;
            border-radius: var(--radius);
            box-shadow: var(--shadow-xl);
            z-index: 9999;
            animation: slideInRight 0.3s ease;
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 600;
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Add notification animations to document
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);

    const searchInput = document.querySelector('.search-box input');

    if (searchInput) {
        let searchTimeout;

        searchInput.addEventListener('input', function (e) {
            clearTimeout(searchTimeout);
            const query = e.target.value.toLowerCase().trim();

            searchTimeout = setTimeout(() => {
                if (query.length > 0) {
                    performSearch(query);
                } else {
                    clearSearch();
                }
            }, 300);
        });
    }

    function performSearch(query) {
        const currentTab = document.querySelector('.content-tab:not([style*="display: none"])');
        if (!currentTab) return;

        const searchableElements = currentTab.querySelectorAll(
            '.course-title-modern, .lesson-title-text, .exam-title-modern'
        );

        let foundCount = 0;

        searchableElements.forEach(element => {
            const card = element.closest('.course-card-modern, .table-row-hover, .exam-card-modern');
            if (!card) return;

            const text = element.textContent.toLowerCase();
            if (text.includes(query)) {
                card.style.display = '';
                card.style.animation = 'highlightPulse 0.5s ease';
                foundCount++;
            } else {
                card.style.display = 'none';
            }
        });

        if (foundCount === 0 && searchableElements.length > 0) {
            showNotification(`Không tìm thấy kết quả cho "${query}"`, 'info');
        }
    }

    function clearSearch() {
        const allCards = document.querySelectorAll(
            '.course-card-modern, .table-row-hover, .exam-card-modern'
        );
        allCards.forEach(card => {
            card.style.display = '';
            card.style.animation = '';
        });
    }


    const examRatings = document.querySelectorAll('.exam-rating-stars');

    examRatings.forEach(rating => {
        const stars = rating.querySelectorAll('i');

        stars.forEach((star, index) => {
            star.addEventListener('mouseenter', function () {
                stars.forEach((s, i) => {
                    if (i <= index) {
                        s.style.transform = 'scale(1.2)';
                        s.style.transition = 'transform 0.2s ease';
                    }
                });
            });

            star.addEventListener('mouseleave', function () {
                stars.forEach(s => {
                    s.style.transform = 'scale(1)';
                });
            });
        });
    });

    const tableRows = document.querySelectorAll('.modern-table tbody tr');

    tableRows.forEach(row => {
        row.addEventListener('mouseenter', function () {
            this.style.transform = 'scale(1.01)';
        });
        row.addEventListener('mouseleave', function () {
            this.style.transform = 'scale(1)';
        });
    });

    const actionButtons = document.querySelectorAll('.action-btn[title]');

    actionButtons.forEach(button => {
        button.addEventListener('mouseenter', function (e) {
            const title = this.getAttribute('title');
            if (!title) return;

            const tooltip = document.createElement('div');
            tooltip.className = 'custom-tooltip';
            tooltip.textContent = title;
            tooltip.style.cssText = `
                position: absolute;
                background: var(--text-primary);
                color: white;
                padding: 6px 12px;
                border-radius: 6px;
                font-size: 12px;
                white-space: nowrap;
                z-index: 10000;
                pointer-events: none;
                opacity: 0;
                transition: opacity 0.2s ease;
                box-shadow: var(--shadow-lg);
            `;

            document.body.appendChild(tooltip);

            const rect = this.getBoundingClientRect();
            tooltip.style.top = (rect.top - tooltip.offsetHeight - 8) + window.scrollY + 'px';
            tooltip.style.left = (rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2)) + window.scrollX + 'px';

            setTimeout(() => tooltip.style.opacity = '1', 10);

            this._tooltip = tooltip;
        });

        button.addEventListener('mouseleave', function () {
            if (this._tooltip) {
                this._tooltip.style.opacity = '0';
                setTimeout(() => this._tooltip.remove(), 200);
                this._tooltip = null;
            }
        });
    });
    function checkVisibility() {
        const elements = document.querySelectorAll('.course-card-modern, .exam-card-modern, .stat-card-modern');

        elements.forEach(element => {
            const rect = element.getBoundingClientRect();
            const isVisible = (rect.top < window.innerHeight - 100) && (rect.bottom > 0);

            if (isVisible && !element.classList.contains('aos-animated')) {
                element.classList.add('aos-animated');
                element.style.animation = 'fadeInUp 0.5s ease forwards';
            }
        });
    }

    // Add AOS animation
    const aosStyle = document.createElement('style');
    aosStyle.textContent = `
        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(30px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        @keyframes highlightPulse {
            0%, 100% { background: var(--bg-primary); }
            50% { background: rgba(99, 102, 241, 0.1); }
        }
    `;
    document.head.appendChild(aosStyle);

    window.addEventListener('scroll', checkVisibility);
    window.addEventListener('load', checkVisibility);

    document.addEventListener('keydown', function (e) {
        // Alt + number to switch tabs
        if (e.altKey && e.key >= '1' && e.key <= '7') {
            e.preventDefault();
            const tabIndex = parseInt(e.key) - 1;
            const tabs = ['courses', 'lessons', 'exams', 'progress', 'statistics', 'examresults', 'settings'];
            if (tabs[tabIndex]) {
                switchTab(tabs[tabIndex]);
            }
        }

        // Escape to close mobile menu
        if (e.key === 'Escape' && sidebar && sidebar.classList.contains('active')) {
            sidebar.classList.remove('active');
        }
    });

    // ========================================
    // CONSOLE LOG
    // ========================================

    console.log('%c🎓 Profile Dashboard Loaded Successfully!', 'color: #6366f1; font-size: 16px; font-weight: bold;');
    console.log('%cKeyboard Shortcuts:', 'color: #10b981; font-weight: bold;');
    console.log('Alt + 1-7: Switch between tabs');
    console.log('Escape: Close mobile menu');

})();
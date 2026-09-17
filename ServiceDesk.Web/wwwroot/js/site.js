// IT Service Desk Copilot - Main Client JavaScript
(function () {
    'use strict';

    function initTheme() {
        var toggleBtn = document.getElementById('themeToggleBtn');
        var topToggleBtn = document.getElementById('themeToggleTop');
        var themeIcon = document.getElementById('themeIcon');
        var topThemeIcon = topToggleBtn ? topToggleBtn.querySelector('i') : null;
        if (!toggleBtn && !topToggleBtn) return;

        function applyThemeUI(theme) {
            var isDark = theme === 'dark';
            if (theme === 'dark') {
                if (themeIcon) { themeIcon.className = 'bi bi-sun-fill'; themeIcon.style.color = '#f59e0b'; }
                if (topThemeIcon) { topThemeIcon.className = 'bi bi-sun-fill'; topThemeIcon.style.color = '#f59e0b'; }
            } else {
                if (themeIcon) { themeIcon.className = 'bi bi-moon-stars-fill'; themeIcon.style.color = '#4f46e5'; }
                if (topThemeIcon) { topThemeIcon.className = 'bi bi-moon-stars-fill'; topThemeIcon.style.color = '#4f46e5'; }
            }
            [toggleBtn, topToggleBtn].filter(Boolean).forEach(function (button) {
                button.setAttribute('title', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
                button.setAttribute('aria-label', isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode');
            });
        }

        // Check current theme
        var currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        applyThemeUI(currentTheme);

        function toggleTheme() {
            var active = document.documentElement.getAttribute('data-theme') || 'light';
            var nextTheme = active === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', nextTheme);
            localStorage.setItem('sdesk_theme', nextTheme);
            applyThemeUI(nextTheme);
        }

        if (toggleBtn) toggleBtn.addEventListener('click', toggleTheme);
        if (topToggleBtn) topToggleBtn.addEventListener('click', toggleTheme);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTheme);
    } else {
        initTheme();
    }
})();

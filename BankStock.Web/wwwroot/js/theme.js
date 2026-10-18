/**
 * BankStock Theme Switcher (Dark / Light mode)
 * Handles instant theme toggling and cookie persistence without flash.
 */
(function () {
    'use strict';

    function getCookie(name) {
        var match = document.cookie.match(new RegExp('(?:^|; )' + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + '=([^;]*)'));
        return match ? decodeURIComponent(match[1]) : null;
    }

    function setCookie(name, value, days) {
        var expires = '';
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = '; expires=' + date.toUTCString();
        }
        document.cookie = name + '=' + encodeURIComponent(value) + expires + '; path=/; SameSite=Lax';
    }

    function getActiveTheme() {
        var cookieTheme = getCookie('theme');
        if (cookieTheme === 'light' || cookieTheme === 'dark') {
            return cookieTheme;
        }
        return 'dark'; // Default fintech theme
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);

        // Update all theme toggle buttons across pages
        var buttons = document.querySelectorAll('[data-action="toggle-theme"]');
        buttons.forEach(function (btn) {
            var icon = btn.querySelector('i');
            if (icon) {
                if (theme === 'light') {
                    icon.className = 'bi bi-moon-stars-fill';
                    btn.setAttribute('title', btn.getAttribute('data-title-to-dark') || btn.title);
                } else {
                    icon.className = 'bi bi-sun-fill';
                    btn.setAttribute('title', btn.getAttribute('data-title-to-light') || btn.title);
                }
            }
        });

        // Trigger custom event for components/charts
        window.dispatchEvent(new CustomEvent('bankstock:themeChanged', { detail: { theme: theme } }));
    }

    function toggleTheme() {
        var current = getActiveTheme();
        var next = (current === 'dark') ? 'light' : 'dark';
        setCookie('theme', next, 365);
        applyTheme(next);
    }

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        var current = getActiveTheme();
        applyTheme(current);

        document.querySelectorAll('[data-action="toggle-theme"]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                toggleTheme();
            });
        });
    });
})();

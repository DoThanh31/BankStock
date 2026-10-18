/**
 * BankStock - đồng bộ Dark/Light ngay khi tải <head> (trước khi vẽ trang) để không nhấp nháy.
 * Server đã gắn data-bs-theme theo cookie; script này chỉ sửa lại nếu cookie vừa đổi phía client.
 * Đặt trong file riêng (không inline) để CSP không cần 'unsafe-inline' cho script.
 */
(function () {
    'use strict';
    var match = document.cookie.match(/(?:^|; )theme=([^;]*)/);
    var theme = match ? decodeURIComponent(match[1]) : document.documentElement.getAttribute('data-bs-theme');
    if (theme !== 'light' && theme !== 'dark') theme = 'dark';
    document.documentElement.setAttribute('data-bs-theme', theme);
    document.documentElement.setAttribute('data-theme', theme);
})();

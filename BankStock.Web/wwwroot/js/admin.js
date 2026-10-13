/**
 * BankStock Admin Layout Interactions
 */
document.addEventListener('DOMContentLoaded', function () {
    var sidebar = document.getElementById('adminSidebar');
    var overlay = document.getElementById('sidebarOverlay');
    var toggleBtn = document.getElementById('sidebarToggleBtn');

    function toggleSidebar() {
        if (!sidebar) return;
        sidebar.classList.toggle('show');
        if (overlay) {
            overlay.classList.toggle('show');
        }
    }

    function closeSidebar() {
        if (!sidebar) return;
        sidebar.classList.remove('show');
        if (overlay) {
            overlay.classList.remove('show');
        }
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function (e) {
            e.preventDefault();
            toggleSidebar();
        });
    }

    if (overlay) {
        overlay.addEventListener('click', function () {
            closeSidebar();
        });
    }
});

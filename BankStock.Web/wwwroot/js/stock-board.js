/**
 * BankStock Price Board Interactive Features
 * Provides instant filtering, searching, and row highlighting.
 */
document.addEventListener('DOMContentLoaded', function () {
    var searchInput = document.getElementById('stockSearchInput');
    var filterButtons = document.querySelectorAll('[data-group-filter]');
    var tableRows = document.querySelectorAll('#stockBoardTable tbody tr[data-stock-row]');
    var noDataRow = document.getElementById('stockNoDataRow');

    var currentGroup = 'all';
    var currentKeyword = '';

    function filterTable() {
        var visibleCount = 0;

        tableRows.forEach(function (row) {
            var code = (row.getAttribute('data-stock-code') || '').toLowerCase();
            var name = (row.getAttribute('data-bank-name') || '').toLowerCase();
            var group = row.getAttribute('data-bank-group') || '';

            var matchesKeyword = !currentKeyword || code.indexOf(currentKeyword) !== -1 || name.indexOf(currentKeyword) !== -1;
            var matchesGroup = currentGroup === 'all' || group === currentGroup;

            if (matchesKeyword && matchesGroup) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        if (noDataRow) {
            noDataRow.style.display = visibleCount === 0 ? '' : 'none';
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            currentKeyword = e.target.value.trim().toLowerCase();
            filterTable();
        });
    }

    filterButtons.forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            filterButtons.forEach(function (b) { b.classList.remove('active'); });
            btn.classList.add('active');

            currentGroup = btn.getAttribute('data-group-filter') || 'all';
            filterTable();
        });
    });
});

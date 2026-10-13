/**
 * BankStock - danh sách lọc / phân trang phía server, chỉ thay phần kết quả bằng AJAX (jQuery).
 * Markup: form#listFilterForm (GET) + div#listResults (partial kết quả). Không có JS thì form GET vẫn chạy.
 * Sự kiện "bankstock:changed" (sau khi thêm/sửa/xóa) → tải lại trang hiện tại của danh sách.
 */
(function ($) {
    'use strict';

    $(function () {
        var $filter = $('#listFilterForm');
        var $results = $('#listResults');
        if (!$results.length) return;

        var currentRequest = null;
        var searchTimer = null;

        // ?reset=true chỉ báo server xóa bộ lọc đã lưu trong Session; không giữ lại trên thanh địa chỉ.
        function historyUrl(url) {
            var parsed = new URL(url, window.location.href);
            parsed.searchParams.delete('reset');
            return parsed.pathname + parsed.search;
        }

        function load(url, pushState) {
            if (currentRequest) currentRequest.abort();
            $results.addClass('is-loading');

            currentRequest = $.ajax({ url: url, method: 'GET', dataType: 'html', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (html) {
                    $results.html(html);
                    if (pushState) {
                        window.history.pushState({ list: true }, '', historyUrl(url));
                        syncFilterFromUrl();
                    }
                })
                .fail(function (_xhr, status) {
                    if (status !== 'abort') window.location.href = url;
                })
                .always(function () {
                    $results.removeClass('is-loading');
                    currentRequest = null;
                });
        }

        function filterUrl() {
            var params = $filter.serializeArray().filter(function (p) { return p.value !== ''; });
            var query = $.param(params);
            return $filter.attr('action') + (query ? '?' + query : '');
        }

        function syncFilterFromUrl() {
            if (!$filter.length) return;
            var params = new URLSearchParams(window.location.search);
            $filter.find('input[type=search], input[type=text], input[type=date]').each(function () {
                var value = params.get(this.name) || '';
                if (this.value !== value) this.value = value;
            });
            $filter.find('select').each(function () {
                var value = params.get(this.name);
                // URL không có pageSize: server dùng page size trong cookie (= lựa chọn đang hiển thị), giữ nguyên.
                if (value === null && this.name === 'pageSize') return;
                this.value = value !== null ? value : $(this).find('option').first().val();
            });
        }

        if ($filter.length) {
            $filter.on('submit', function (e) {
                e.preventDefault();
                load(filterUrl(), true);
            });
            $filter.on('change', 'select, input[type=date]', function () {
                $filter.trigger('submit');
            });
            $filter.on('input', 'input[type=search]', function () {
                clearTimeout(searchTimer);
                searchTimer = setTimeout(function () { $filter.trigger('submit'); }, 350);
            });
            $('#filterReset').on('click', function (e) {
                e.preventDefault();
                load(this.href, true);
            });
        }

        $results.on('click', '.pagination a.page-link', function (e) {
            e.preventDefault();
            var href = $(this).attr('href');
            if (href && href !== '#') load(href, true);
        });

        window.addEventListener('popstate', function () {
            syncFilterFromUrl();
            load(window.location.pathname + window.location.search, false);
        });

        $(document).on('bankstock:changed', function () {
            load(window.location.pathname + window.location.search, false);
        });
    });
})(jQuery);

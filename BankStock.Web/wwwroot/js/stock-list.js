/**
 * BankStock - Danh sách cổ phiếu.
 * Lọc / sắp xếp / phân trang chạy phía server; script này chỉ tải lại phần kết quả bằng AJAX
 * (jQuery) và đồng bộ URL qua history API. Không có JS thì form GET vẫn hoạt động bình thường.
 */
(function ($) {
    'use strict';

    $(function () {
        var $form = $('#stockFilterForm');
        var $results = $('#stockListResults');
        if (!$form.length || !$results.length) return;

        var currentRequest = null;
        var searchTimer = null;

        // ?reset=true chỉ báo server xóa bộ lọc đã lưu trong Session; không giữ lại trên thanh địa chỉ.
        function historyUrl(url) {
            var parsed = new URL(url, window.location.href);
            parsed.searchParams.delete('reset');
            return parsed.pathname + parsed.search;
        }

        function buildUrl() {
            // Bỏ tham số rỗng để URL gọn; trang luôn quay về 1 khi đổi bộ lọc.
            var params = $form.serializeArray().filter(function (p) { return p.value !== ''; });
            var query = $.param(params);
            return $form.attr('action') + (query ? '?' + query : '');
        }

        function load(url, pushState) {
            if (currentRequest) currentRequest.abort();
            $results.addClass('is-loading');

            currentRequest = $.ajax({
                url: url,
                method: 'GET',
                dataType: 'html',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).done(function (html) {
                $results.html(html);
                if (pushState) {
                    window.history.pushState({ stockList: true }, '', historyUrl(url));
                    // Link sắp xếp / reset đổi URL mà không qua form, nên cập nhật lại các ô lọc.
                    syncFormFromUrl();
                }
            }).fail(function (_xhr, status) {
                if (status !== 'abort') {
                    window.location.href = url;
                }
            }).always(function () {
                $results.removeClass('is-loading');
                currentRequest = null;
            });
        }

        function syncFormFromUrl() {
            var params = new URLSearchParams(window.location.search);
            $form.find('input[type=search], input[type=text]').each(function () {
                var value = params.get(this.name) || '';
                if (this.value !== value) this.value = value; // tránh nhảy con trỏ khi đang gõ
            });
            $form.find('select').each(function () {
                var value = params.get(this.name);
                // URL không có pageSize: server dùng page size trong cookie (= lựa chọn đang hiển thị), giữ nguyên.
                if (value === null && this.name === 'pageSize') return;
                this.value = value !== null ? value : $(this).find('option').first().val();
            });
            $form.find('input[type=checkbox]').each(function () {
                this.checked = params.get(this.name) === 'true';
            });
        }

        $form.on('submit', function (e) {
            e.preventDefault();
            load(buildUrl(), true);
        });

        $form.on('change', 'select, input[type=checkbox]', function () {
            $form.trigger('submit');
        });

        $form.on('input', 'input[type=search]', function () {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () { $form.trigger('submit'); }, 350);
        });

        $('#filterReset').on('click', function (e) {
            e.preventDefault();
            load(this.href, true);
        });

        // Phân trang và tiêu đề cột sắp xếp nằm trong partial được thay thế, nên dùng event delegation.
        $results.on('click', '.pagination a.page-link, a.sort-link', function (e) {
            var href = $(this).attr('href');
            if (!href || href === '#') {
                e.preventDefault();
                return;
            }
            e.preventDefault();
            load(href, true);
        });

        window.addEventListener('popstate', function () {
            syncFormFromUrl();
            load(window.location.pathname + window.location.search, false);
        });
    });
})(jQuery);

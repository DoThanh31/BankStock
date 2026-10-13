/**
 * BankStock - Đặt lệnh mua/bán (jQuery + SweetAlert2).
 * Bấm Mua/Bán → xác nhận → AJAX POST /Investor/Trade/Place → SweetAlert2 báo kết quả,
 * cập nhật ngay tiền/cổ phiếu khả dụng và danh sách lệnh chờ. Server kiểm tra lại toàn bộ.
 * Giỏ lệnh nháp (Session): "Lưu nháp" lưu form hiện tại, "Dùng" điền lệnh nháp vào form để đặt.
 */
(function ($) {
    'use strict';

    $(function () {
        var ui = window.BankStockUi;
        var $form = $('#orderForm');
        if (!$form.length || !ui) return;

        var locale = document.documentElement.lang === 'vi' ? 'vi-VN' : 'en-US';
        var money = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
        var decimal = new Intl.NumberFormat(locale, { maximumFractionDigits: 2 });
        var quote = JSON.parse($('#tradeQuoteData').text() || 'null');

        var $code = $form.find('[name=StockCode]');
        var $qty = $form.find('[name=Quantity]');
        var $price = $form.find('[name=Price]');
        var $submit = $('#orderSubmit');
        var $pending = $('#pendingOrders');
        var $drafts = $('#orderDrafts');
        var $draftId = $form.find('[name=DraftId]');

        function side() { return $form.find('[name=Type]:checked').val(); }
        function mode() { return $form.find('[name=Mode]:checked').val(); }
        function isBuy() { return side() === 'Buy'; }

        function orderPrice() {
            return mode() === 'Market' ? quote.matchPrice : (Number($price.val()) || 0);
        }

        function feeOf(value) {
            return Math.round(value * quote.feePercent / 100);
        }

        function renderQuote() {
            $('[data-quote]').each(function () {
                var key = $(this).data('quote');
                var value = quote[key];
                if (typeof value === 'number') {
                    $(this).text(key === 'feePercent' ? decimal.format(value) : money.format(value));
                } else if (key === 'tradeDate' && value) {
                    $(this).text(new Date(value).toLocaleDateString(locale));
                } else {
                    $(this).text(value || '');
                }
            });
            var $link = $('[data-quote-link]');
            $link.attr('href', $link.attr('href').replace(/[A-Z]{3}$/, quote.stockCode));
            $price.attr({ min: quote.floorPrice, max: quote.ceilingPrice });
        }

        function renderSide() {
            var buy = isBuy();
            $submit.removeClass($submit.data('class-buy') + ' ' + $submit.data('class-sell'))
                .addClass(buy ? $submit.data('class-buy') : $submit.data('class-sell'));
            $submit.find('span').text(buy ? $submit.data('text-buy') : $submit.data('text-sell'));
            ['[data-estimate=totalLabel]', '[data-available-label]'].forEach(function (selector) {
                var $label = $(selector);
                $label.text(buy ? $label.data('label-buy') : $label.data('label-sell'));
            });
            recalc();
        }

        function renderMode() {
            if (mode() === 'Market') {
                $price.val(quote.matchPrice).prop('disabled', true);
            } else {
                $price.prop('disabled', false);
                var current = Number($price.val());
                if (!current || current < quote.floorPrice || current > quote.ceilingPrice) $price.val(quote.matchPrice);
            }
            recalc();
        }

        function maxQuantity() {
            var lot = quote.lotSize;
            if (isBuy()) {
                var unitCost = orderPrice() * (1 + quote.feePercent / 100);
                return unitCost > 0 ? Math.floor(quote.availableCash / unitCost / lot) * lot : 0;
            }
            return Math.floor(quote.availableQuantity / lot) * lot;
        }

        function recalc() {
            var qty = Number($qty.val()) || 0;
            var value = qty * orderPrice();
            var fee = feeOf(value);
            var total = isBuy() ? value + fee : value - fee;
            $('[data-estimate=value]').text(money.format(value));
            $('[data-estimate=fee]').text(money.format(fee));
            $('[data-estimate=total]').text(money.format(total));
            $('[data-estimate=available]').text(isBuy() ? money.format(quote.availableCash) : money.format(quote.availableQuantity));
            $('[data-estimate=max]').text(money.format(maxQuantity()));
        }

        function loadQuote(code) {
            return $.getJSON($form.data('quote-url'), { code: code })
                .done(function (data) {
                    quote = data;
                    renderQuote();
                    renderMode();
                })
                .fail(ui.showError);
        }

        function reloadPending() {
            $.ajax({ url: $form.data('pending-url'), method: 'GET', dataType: 'html', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (html) { $pending.html(html); });
        }

        function reloadDrafts() {
            $.ajax({ url: $form.data('drafts-url'), method: 'GET', dataType: 'html', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (html) { $drafts.html(html); });
        }

        $form.on('change', '[name=Type]', renderSide);
        $form.on('change', '[name=Mode]', renderMode);
        $form.on('input change', '[name=Quantity], [name=Price]', recalc);

        $code.on('change', function () {
            var code = this.value;
            loadQuote(code);
            var url = new URL(window.location.href);
            url.searchParams.set('code', code);
            window.history.replaceState(null, '', url);
        });

        $form.on('click', '[data-quantity-step]', function () {
            var next = (Number($qty.val()) || 0) + Number($(this).data('quantity-step')) * quote.lotSize;
            $qty.val(Math.max(quote.lotSize, next)).trigger('change');
        });

        $form.on('click', '[data-estimate=max]', function (e) {
            e.preventDefault();
            var max = maxQuantity();
            if (max > 0) $qty.val(max).trigger('change');
        });

        $form.on('submit', function (e) {
            e.preventDefault();
            if ($form.valid && !$form.valid()) return;

            var qty = Number($qty.val()) || 0;
            var price = orderPrice();
            var value = qty * price;
            var total = isBuy() ? value + feeOf(value) : value - feeOf(value);
            var sideLabel = isBuy() ? $form.data('label-buy') : $form.data('label-sell');
            var priceText = mode() === 'Market' ? $form.data('label-market') + ' (~' + money.format(price) + ')' : money.format(price);
            var summary = sideLabel + ' ' + money.format(qty) + ' ' + quote.stockCode + ' @ ' + priceText +
                ' — ' + $('[data-estimate=totalLabel]').text() + ': ' + money.format(total) + ' VNĐ';

            ui.confirm(summary, $submit.find('span').text(), 'question', isBuy() ? '#198754' : '#dc3545').then(function (result) {
                if (!result.isConfirmed) return;
                $submit.prop('disabled', true);
                $.ajax({ url: $form.attr('action'), method: 'POST', data: $form.serialize(), headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                    .done(function (res) {
                        Swal.fire({ icon: res.order.status === 'Matched' ? 'success' : 'info', title: res.message });
                        loadQuote(quote.stockCode);
                        reloadPending();
                        $draftId.val('');
                        if (res.draftUsed) reloadDrafts();
                    })
                    .fail(ui.showError)
                    .always(function () { $submit.prop('disabled', false); });
            });
        });

        // Lưu form hiện tại vào giỏ lệnh nháp (Session phía server), chưa giam tiền / cổ phiếu.
        $('#draftSave').on('click', function () {
            if ($form.valid && !$form.valid()) return;
            var $btn = $(this).prop('disabled', true);
            $.ajax({ url: $form.data('save-draft-url'), method: 'POST', data: $form.serialize(), headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (res) {
                    ui.toast('success', res.message);
                    reloadDrafts();
                })
                .fail(ui.showError)
                .always(function () { $btn.prop('disabled', false); });
        });

        // "Dùng" lệnh nháp: điền vào form (đổi mã thì tải báo giá trước rồi mới đặt giá Limit).
        $drafts.on('click', '[data-draft-use]', function () {
            var draft = $(this).data();
            $form.find('[name=Type][value=' + draft.type + ']').prop('checked', true);
            $form.find('[name=Mode][value=' + draft.mode + ']').prop('checked', true);
            $qty.val(draft.quantity);
            $draftId.val(draft.draftUse);

            function applyPrice() {
                if (draft.mode === 'Limit' && draft.price) $price.val(draft.price);
                renderMode();
                renderSide();
            }

            if (draft.code !== quote.stockCode) {
                $code.val(draft.code);
                loadQuote(draft.code).done(applyPrice);
            } else {
                applyPrice();
            }
            ui.toast('info', $form.data('draft-loaded'));
            $form[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
        });

        // Hủy lệnh chờ / xóa lệnh nháp (ui-common.js) xong thì cập nhật lại số dư và các danh sách.
        $(document).on('bankstock:changed', function () {
            loadQuote(quote.stockCode);
            reloadPending();
            reloadDrafts();
        });

        renderQuote();
        renderMode();
        renderSide();
    });
})(jQuery);

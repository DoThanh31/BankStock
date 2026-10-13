/**
 * BankStock - Ví ảo: nạp / rút tiền giả lập bằng AJAX. Thành công → SweetAlert2 + cập nhật số dư ngay
 * và tải lại lịch sử giao dịch (ajax-list.js lắng nghe sự kiện "bankstock:changed").
 */
(function ($) {
    'use strict';

    $(function () {
        var ui = window.BankStockUi;
        var $form = $('#walletTransferForm');
        if (!$form.length || !ui) return;

        var locale = document.documentElement.lang === 'vi' ? 'vi-VN' : 'en-US';
        var money = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
        var $amount = $form.find('[name=Amount]');

        function renderWallet(wallet) {
            if (!wallet) return;
            $('[data-wallet]').each(function () {
                var value = wallet[$(this).data('wallet')];
                if (typeof value === 'number') $(this).text(money.format(value));
            });
        }

        function send(url) {
            var $buttons = $form.find('[type=submit]').prop('disabled', true);
            $.ajax({ url: url, method: 'POST', data: $form.serialize(), headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .done(function (res) {
                    Swal.fire({ icon: 'success', title: res.message, timer: 2500, showConfirmButton: false });
                    renderWallet(res.wallet);
                    $amount.val('');
                    ui.changed();
                })
                .fail(ui.showError)
                .always(function () { $buttons.prop('disabled', false); });
        }

        $form.on('click', '[data-amount-preset]', function () {
            $amount.val($(this).data('amount-preset')).trigger('change');
        });

        $form.on('submit', function (e) {
            e.preventDefault();
            if ($form.valid && !$form.valid()) return;

            var submitter = e.originalEvent && e.originalEvent.submitter;
            var url = submitter ? submitter.getAttribute('formaction') : $form.attr('action');
            var confirmText = submitter && submitter.getAttribute('data-confirm-withdraw');
            if (!confirmText) {
                send(url);
                return;
            }

            ui.confirm(confirmText.replace('{0}', money.format(Number($amount.val()) || 0)), submitter.textContent.trim())
                .then(function (result) { if (result.isConfirmed) send(url); });
        });
    });
})(jQuery);

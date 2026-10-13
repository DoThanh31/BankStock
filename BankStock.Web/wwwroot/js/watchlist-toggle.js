/**
 * BankStock - Ghim / bỏ ghim mã vào Watchlist (jQuery AJAX → POST /api/v1/watchlist/toggle).
 * Nút: [data-watchlist-toggle="VCB"] + data-url, data-label-add, data-label-remove.
 */
(function ($) {
    'use strict';

    $(document).on('click', '[data-watchlist-toggle]', function () {
        var ui = window.BankStockUi;
        var $btn = $(this).prop('disabled', true);

        $.ajax({
            url: $btn.data('url'),
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ stockCode: $btn.data('watchlist-toggle') }),
            headers: { 'RequestVerificationToken': ui.token(), 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function (res) {
            var on = !!res.inWatchlist;
            $btn.attr('aria-pressed', on ? 'true' : 'false');
            $btn.find('i').toggleClass('bi-star-fill', on).toggleClass('bi-star', !on);
            $btn.find('span').text(on ? $btn.data('label-remove') : $btn.data('label-add'));
            ui.toast('success', on ? ui.i18n.watchlistAdded : ui.i18n.watchlistRemoved);
        }).fail(ui.showError)
            .always(function () { $btn.prop('disabled', false); });
    });
})(jQuery);

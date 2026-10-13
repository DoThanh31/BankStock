/**
 * BankStock Admin - CRUD bằng modal + AJAX (jQuery). Dùng chung với ui-common.js (SweetAlert2, token)
 * và ajax-list.js (lọc / phân trang danh sách).
 *
 * Quy ước markup:
 *  - [data-modal-url]      : GET partial form/nội dung vào #adminModal.
 *  - form[data-ajax-form]  : trong modal, submit AJAX. 200 JSON {message} → đóng modal, toast, tải lại danh sách;
 *                            400 HTML → hiển thị lại form kèm lỗi validation từ server.
 *  - [data-modal-reload]   : đổi giá trị thì nạp lại modal với tham số mới (vd. đổi ngày phiên giá).
 *  - [data-band-url]       : tính giá trần/sàn theo biên độ sàn từ giá tham chiếu.
 */
(function ($) {
    'use strict';

    $(function () {
        var ui = window.BankStockUi;
        var modalEl = document.getElementById('adminModal');
        var $modalContent = $('#adminModalContent');
        if (!modalEl || !ui) return;

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        var ajaxHeaders = { 'X-Requested-With': 'XMLHttpRequest' };

        function setModalContent(html) {
            $modalContent.html(html);
            var $form = $modalContent.find('form');
            if ($form.length && $.validator && $.validator.unobtrusive) {
                $form.removeData('validator').removeData('unobtrusiveValidation');
                $.validator.unobtrusive.parse($form);
            }
        }

        function openModal(url) {
            $.ajax({ url: url, method: 'GET', dataType: 'html', headers: ajaxHeaders })
                .done(function (html) {
                    setModalContent(html);
                    modal.show();
                })
                .fail(ui.showError);
        }

        modalEl.addEventListener('shown.bs.modal', function () {
            $modalContent.find('input:not([type=hidden]):not([readonly]), select, textarea').first().trigger('focus');
        });

        $(document).on('click', '[data-modal-url]', function (e) {
            e.preventDefault();
            openModal($(this).data('modal-url'));
        });

        $modalContent.on('submit', 'form[data-ajax-form]', function (e) {
            e.preventDefault();
            var $form = $(this);
            if ($form.valid && !$form.valid()) return;

            var $submit = $form.find('[type=submit]').prop('disabled', true);
            $.ajax({ url: $form.attr('action'), method: 'POST', data: $form.serialize(), headers: ajaxHeaders })
                .done(function (res) {
                    modal.hide();
                    ui.toast('success', res && res.message);
                    ui.changed();
                })
                .fail(function (xhr) {
                    var contentType = xhr.getResponseHeader('Content-Type') || '';
                    if (xhr.status === 400 && contentType.indexOf('text/html') >= 0) {
                        setModalContent(xhr.responseText);
                    } else {
                        ui.showError(xhr);
                    }
                })
                .always(function () {
                    $submit.prop('disabled', false);
                });
        });

        $modalContent.on('change', '[data-modal-reload]', function () {
            var url = $(this).data('modal-reload');
            url += (url.indexOf('?') >= 0 ? '&' : '?') + encodeURIComponent(this.name) + '=' + encodeURIComponent(this.value);
            $.ajax({ url: url, method: 'GET', dataType: 'html', headers: ajaxHeaders })
                .done(setModalContent)
                .fail(ui.showError);
        });

        $modalContent.on('click', '[data-band-url]', function () {
            var $form = $(this).closest('form');
            var stockId = $form.find('[name=StockId]').val();
            var refPrice = $form.find('[name=RefPrice]').val();
            if (!stockId || stockId === '0' || !refPrice) {
                ui.toast('warning', ui.i18n.bandMissing);
                return;
            }

            $.ajax({ url: $(this).data('band-url'), method: 'GET', data: { stockId: stockId, refPrice: refPrice }, dataType: 'json', headers: ajaxHeaders })
                .done(function (band) {
                    $form.find('[name=CeilingPrice]').val(band.ceilingPrice).trigger('change');
                    $form.find('[name=FloorPrice]').val(band.floorPrice).trigger('change');
                })
                .fail(ui.showError);
        });
    });
})(jQuery);

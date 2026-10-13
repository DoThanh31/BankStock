/**
 * BankStock - tiện ích giao diện dùng chung (Admin + Investor): SweetAlert2 toast/lỗi,
 * token antiforgery cho AJAX, chuỗi đa ngôn ngữ đọc từ #jsI18n.
 *
 *  - [data-confirm-post] : hỏi xác nhận rồi POST kèm header RequestVerificationToken; thành công phát
 *                          sự kiện "bankstock:changed" để danh sách tự tải lại.
 *  - [data-uppercase]    : tự chuyển chữ hoa khi gõ.
 */
(function ($) {
    'use strict';

    var i18n = $.extend({
        confirmTitle: 'Are you sure?',
        confirmYes: 'OK',
        cancel: 'Cancel',
        errorTitle: 'Error',
        errorGeneric: 'The operation failed.'
    }, $('#jsI18n').data() || {});

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    }

    var Toast = window.Swal ? Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 2500,
        timerProgressBar: true
    }) : null;

    function toast(icon, title) {
        if (Toast) Toast.fire({ icon: icon, title: title });
    }

    function errorMessage(xhr) {
        var json = xhr && xhr.responseJSON;
        return (json && (json.message || json.detail || json.title)) || i18n.errorGeneric;
    }

    function showError(xhr) {
        if (window.Swal) {
            Swal.fire({ icon: 'error', title: i18n.errorTitle, text: errorMessage(xhr) });
        } else {
            window.alert(errorMessage(xhr));
        }
    }

    /** POST AJAX kèm token antiforgery và header nhận diện AJAX. */
    function post(url, data, options) {
        return $.ajax($.extend({
            url: url,
            method: 'POST',
            data: data,
            headers: { 'RequestVerificationToken': token(), 'X-Requested-With': 'XMLHttpRequest' }
        }, options || {}));
    }

    function confirm(text, confirmButton, icon, confirmColor) {
        return Swal.fire({
            icon: icon || 'warning',
            title: i18n.confirmTitle,
            text: text,
            showCancelButton: true,
            confirmButtonText: confirmButton || i18n.confirmYes,
            cancelButtonText: i18n.cancel,
            confirmButtonColor: confirmColor || '#dc3545',
            reverseButtons: true,
            focusCancel: true
        });
    }

    window.BankStockUi = {
        i18n: i18n,
        token: token,
        toast: toast,
        showError: showError,
        post: post,
        confirm: confirm,
        changed: function () { $(document).trigger('bankstock:changed'); }
    };

    $(document).on('click', '[data-confirm-post]', function (e) {
        e.preventDefault();
        var $btn = $(this);
        confirm($btn.data('confirm-text'), $btn.data('confirm-button'), $btn.data('confirm-icon')).then(function (result) {
            if (!result.isConfirmed) return;
            post($btn.data('confirm-post'))
                .done(function (res) {
                    toast('success', res && res.message);
                    window.BankStockUi.changed();
                })
                .fail(showError);
        });
    });

    $(document).on('input', '[data-uppercase]', function () {
        var pos = this.selectionStart;
        this.value = this.value.toUpperCase();
        if (pos !== null) this.setSelectionRange(pos, pos);
    });
})(jQuery);

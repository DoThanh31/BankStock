/**
 * Mở rộng jQuery Validation (nạp ngay sau jquery.validate.unobtrusive):
 *  - Thông báo mặc định cho luật sinh từ thuộc tính HTML5 (min, max, step, number) theo ngôn ngữ hiện tại,
 *    lấy từ #jsI18n (thông báo data-val-* của server vẫn được ưu tiên dùng cho luật tương ứng).
 *  - Adapter cho [LotSize]: khối lượng phải là bội số của data-val-lotsize-multiple.
 */
(function ($) {
    'use strict';
    if (!$.validator) return;

    var i18n = $('#jsI18n').data() || {};
    var messages = {};
    if (i18n.valMin) messages.min = $.validator.format(i18n.valMin);
    if (i18n.valMax) messages.max = $.validator.format(i18n.valMax);
    if (i18n.valStep) messages.step = $.validator.format(i18n.valStep);
    if (i18n.valNumber) messages.number = i18n.valNumber;
    $.extend($.validator.messages, messages);

    $.validator.addMethod('lotsize', function (value, element, multiple) {
        if (this.optional(element)) return true;
        var quantity = Number(value);
        return Number.isInteger(quantity) && quantity % Number(multiple) === 0;
    });

    if ($.validator.unobtrusive) {
        $.validator.unobtrusive.adapters.addSingleVal('lotsize', 'multiple');
    }
})(jQuery);

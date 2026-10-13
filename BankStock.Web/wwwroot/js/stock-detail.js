/**
 * BankStock - Biểu đồ nến trang chi tiết cổ phiếu (Lightweight Charts v4).
 * Dữ liệu lấy qua AJAX từ GET /api/v1/stocks/{code}/history?timeframe=1D|1W|1M|1Y.
 * Màu lấy từ CSS variables nên tự đổi theo Dark/Light mode.
 */
(function ($) {
    'use strict';

    $(function () {
        var container = document.getElementById('priceChart');
        var $status = $('#priceChartStatus');
        var $tabs = $('#timeframeTabs [data-timeframe]');
        if (!container) return;

        var msgEmpty = container.getAttribute('data-msg-empty') || '';
        var msgError = container.getAttribute('data-msg-error') || '';

        if (!window.LightweightCharts) {
            $status.text(msgError);
            return;
        }

        function cssVar(name) {
            return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        }

        function chartOptions() {
            return {
                layout: {
                    background: { type: 'solid', color: cssVar('--bg-card') },
                    textColor: cssVar('--text-secondary'),
                    fontFamily: cssVar('--font-sans')
                },
                grid: {
                    vertLines: { color: cssVar('--border-subtle') },
                    horzLines: { color: cssVar('--border-subtle') }
                },
                rightPriceScale: { borderColor: cssVar('--border-color') },
                timeScale: { borderColor: cssVar('--border-color') }
            };
        }

        function candleOptions() {
            var up = cssVar('--stock-up');
            var down = cssVar('--stock-down');
            return {
                upColor: up, downColor: down,
                borderUpColor: up, borderDownColor: down,
                wickUpColor: up, wickDownColor: down
            };
        }

        var locale = document.documentElement.lang === 'vi' ? 'vi-VN' : 'en-US';
        var numberFormat = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });

        var chart = LightweightCharts.createChart(container, $.extend(true, {
            autoSize: true,
            localization: {
                locale: locale,
                priceFormatter: function (price) { return numberFormat.format(price); }
            },
            crosshair: { mode: LightweightCharts.CrosshairMode.Normal }
        }, chartOptions()));

        var candleSeries = chart.addCandlestickSeries($.extend({
            priceFormat: { type: 'price', precision: 0, minMove: 1 }
        }, candleOptions()));
        candleSeries.priceScale().applyOptions({ scaleMargins: { top: 0.08, bottom: 0.28 } });

        var volumeSeries = chart.addHistogramSeries({
            priceFormat: { type: 'volume' },
            priceScaleId: ''
        });
        volumeSeries.priceScale().applyOptions({ scaleMargins: { top: 0.78, bottom: 0 } });

        var lastCandles = [];

        function render(candles) {
            var up = cssVar('--stock-up');
            var down = cssVar('--stock-down');

            candleSeries.setData(candles.map(function (c) {
                return { time: c.time, open: c.open, high: c.high, low: c.low, close: c.close };
            }));
            volumeSeries.setData(candles.map(function (c) {
                // Thêm alpha (hex 8 ký tự) để cột khối lượng không lấn át nến.
                return { time: c.time, value: c.volume, color: (c.close >= c.open ? up : down) + '66' };
            }));
            chart.timeScale().fitContent();
        }

        function load(timeframe) {
            var url = container.getAttribute('data-history-url') + '?timeframe=' + encodeURIComponent(timeframe);
            $status.text('');

            $.getJSON(url).done(function (data) {
                lastCandles = (data && data.candles) || [];
                render(lastCandles);
                if (!lastCandles.length) $status.text(msgEmpty);
            }).fail(function () {
                lastCandles = [];
                render(lastCandles);
                $status.text(msgError);
            });
        }

        $tabs.on('click', function () {
            var $btn = $(this);
            $tabs.removeClass('active').attr('aria-pressed', 'false');
            $btn.addClass('active').attr('aria-pressed', 'true');
            load($btn.data('timeframe'));
        });

        // Đổi theme (theme.js đổi data-bs-theme trên <html>) thì vẽ lại màu biểu đồ.
        new MutationObserver(function () {
            chart.applyOptions(chartOptions());
            candleSeries.applyOptions(candleOptions());
            render(lastCandles);
        }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

        load(container.getAttribute('data-default-timeframe') || '1M');
    });
})(jQuery);

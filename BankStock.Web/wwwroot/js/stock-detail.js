/**
 * BankStock - Biểu đồ giá trang chi tiết cổ phiếu (Lightweight Charts v4): nến hoặc miền, kèm khối lượng.
 * Dữ liệu lấy qua AJAX từ GET /api/v1/stocks/{code}/history?timeframe=1D|1W|1M|1Y.
 * Màu lấy từ CSS variables nên tự đổi theo Dark/Light mode.
 * Loại biểu đồ (Nến | Miền) được lưu trong localStorage.
 */
(function ($) {
    'use strict';

    var CHART_TYPE_KEY = 'bankstock.priceChartType';
    var CHART_TYPES = ['candle', 'area'];

    function readChartType() {
        try {
            var saved = window.localStorage.getItem(CHART_TYPE_KEY);
            if (CHART_TYPES.indexOf(saved) >= 0) return saved;
        } catch (e) { /* localStorage bị chặn (private mode...) */ }
        return CHART_TYPES[0];
    }

    function saveChartType(type) {
        try { window.localStorage.setItem(CHART_TYPE_KEY, type); } catch (e) { /* bỏ qua */ }
    }

    $(function () {
        var container = document.getElementById('priceChart');
        var $status = $('#priceChartStatus');
        var $tabs = $('#timeframeTabs [data-timeframe]');
        var $typeTabs = $('#chartTypeTabs [data-chart-type]');
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

        // Xanh khi giá đóng cửa cuối kỳ >= đầu kỳ, ngược lại đỏ; vùng tô gradient nhạt dần xuống đáy.
        function areaOptions(candles) {
            var rising = candles.length < 2 || candles[candles.length - 1].close >= candles[0].close;
            var color = cssVar(rising ? '--stock-up' : '--stock-down');
            return {
                lineColor: color,
                topColor: color + '59',
                bottomColor: color + '05',
                lineWidth: 2,
                crosshairMarkerBackgroundColor: color
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

        var volumeSeries = chart.addHistogramSeries({
            priceFormat: { type: 'volume' },
            priceScaleId: ''
        });
        volumeSeries.priceScale().applyOptions({ scaleMargins: { top: 0.78, bottom: 0 } });

        var chartType = readChartType();
        var priceSeries = null;
        var lastCandles = [];

        // Chỉ thay series giá; chart, series khối lượng và khung thời gian giữ nguyên.
        function createPriceSeries(type) {
            var base = { priceFormat: { type: 'price', precision: 0, minMove: 1 } };
            var series = type === 'area'
                ? chart.addAreaSeries($.extend(base, areaOptions(lastCandles)))
                : chart.addCandlestickSeries($.extend(base, candleOptions()));
            series.priceScale().applyOptions({ scaleMargins: { top: 0.08, bottom: 0.28 } });
            return series;
        }

        function render(candles) {
            var up = cssVar('--stock-up');
            var down = cssVar('--stock-down');

            if (chartType === 'area') {
                priceSeries.applyOptions(areaOptions(candles));
                priceSeries.setData(candles.map(function (c) {
                    return { time: c.time, value: c.close };
                }));
            } else {
                priceSeries.applyOptions(candleOptions());
                priceSeries.setData(candles.map(function (c) {
                    return { time: c.time, open: c.open, high: c.high, low: c.low, close: c.close };
                }));
            }
            volumeSeries.setData(candles.map(function (c) {
                // Thêm alpha (hex 8 ký tự) để cột khối lượng không lấn át biểu đồ giá.
                return { time: c.time, value: c.volume, color: (c.close >= c.open ? up : down) + '66' };
            }));
        }

        function setActive($group, $btn) {
            $group.removeClass('active').attr('aria-pressed', 'false');
            $btn.addClass('active').attr('aria-pressed', 'true');
        }

        function setChartType(type) {
            setActive($typeTabs, $typeTabs.filter('[data-chart-type="' + type + '"]'));
            if (priceSeries && type === chartType) return;

            var visibleRange = priceSeries ? chart.timeScale().getVisibleLogicalRange() : null;
            if (priceSeries) chart.removeSeries(priceSeries);
            chartType = type;
            priceSeries = createPriceSeries(type);
            render(lastCandles);
            if (visibleRange) chart.timeScale().setVisibleLogicalRange(visibleRange);
        }

        function load(timeframe) {
            var url = container.getAttribute('data-history-url') + '?timeframe=' + encodeURIComponent(timeframe);
            $status.text('');

            $.getJSON(url).done(function (data) {
                lastCandles = (data && data.candles) || [];
                render(lastCandles);
                chart.timeScale().fitContent();
                if (!lastCandles.length) $status.text(msgEmpty);
            }).fail(function () {
                lastCandles = [];
                render(lastCandles);
                $status.text(msgError);
            });
        }

        $tabs.on('click', function () {
            var $btn = $(this);
            setActive($tabs, $btn);
            load($btn.data('timeframe'));
        });

        $typeTabs.on('click', function () {
            var type = $(this).data('chart-type');
            saveChartType(type);
            setChartType(type);
        });

        // Đổi theme (theme.js đổi data-bs-theme trên <html>) thì vẽ lại màu biểu đồ.
        new MutationObserver(function () {
            chart.applyOptions(chartOptions());
            render(lastCandles);
        }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

        setChartType(chartType);
        load(container.getAttribute('data-default-timeframe') || '1M');
    });
})(jQuery);

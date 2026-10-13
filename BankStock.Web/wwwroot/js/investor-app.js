/**
 * BankStock - AngularJS 1.8 cho Dashboard và Watchlist của Investor.
 * Dữ liệu lấy qua REST API bằng $http (cookie auth); thao tác ghi gửi header RequestVerificationToken.
 * ng-app chỉ gắn vào khối nội dung trang (không bao trùm dữ liệu do server render) để tránh template injection.
 */
(function (angular) {
    'use strict';

    var locale = document.documentElement.lang === 'vi' ? 'vi-VN' : 'en-US';
    var moneyFormat = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
    var percentFormat = new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    var STATUS_CLASS = { Ceiling: 'stock-ceil', Floor: 'stock-floor', Up: 'stock-up', Down: 'stock-down', Ref: 'stock-ref' };

    function signed(value, text) {
        return (value > 0 ? '+' : '') + text;
    }

    function showError(response) {
        if (window.BankStockUi) {
            window.BankStockUi.showError({ responseJSON: response && response.data });
        }
    }

    function cssVar(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    var app = angular.module('bankStockInvestor', []);

    app.config(['$httpProvider', function ($httpProvider) {
        $httpProvider.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';
    }]);

    app.run(['$http', function ($http) {
        var tokenInput = document.querySelector('#jsAntiForgery input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            $http.defaults.headers.common.RequestVerificationToken = tokenInput.value;
        }
    }]);

    app.filter('money', function () {
        return function (value) { return typeof value === 'number' ? moneyFormat.format(value) : '—'; };
    });
    app.filter('signedMoney', function () {
        return function (value) { return typeof value === 'number' ? signed(value, moneyFormat.format(value)) : '—'; };
    });
    app.filter('signedPercent', function () {
        return function (value) { return typeof value === 'number' ? signed(value, percentFormat.format(value)) + '%' : '—'; };
    });

    /** Đọc URL API từ data-* của phần tử gắn ng-app (Razor sinh bằng Url.Content). */
    app.factory('apiUrls', ['$document', function ($document) {
        var root = $document[0].querySelector('[ng-app]');
        return {
            summary: root.getAttribute('data-summary-url'),
            watchlist: root.getAttribute('data-watchlist-url'),
            stocks: root.getAttribute('data-stocks-url'),
            trade: root.getAttribute('data-trade-url'),
            detail: root.getAttribute('data-detail-url')
        };
    }]);

    app.factory('watchlistApi', ['$http', 'apiUrls', function ($http, apiUrls) {
        return {
            list: function () { return $http.get(apiUrls.watchlist).then(function (r) { return r.data; }); },
            toggle: function (code) {
                return $http.post(apiUrls.watchlist + '/toggle', { stockCode: code }).then(function (r) { return r.data; });
            },
            remove: function (code) { return $http.delete(apiUrls.watchlist + '/' + encodeURIComponent(code)); }
        };
    }]);

    /** Hàm dùng chung cho template: lớp màu theo trạng thái giá, link đặt lệnh / chi tiết. */
    function decorate(vm, apiUrls) {
        vm.statusClass = function (status) { return STATUS_CLASS[status] || 'stock-ref'; };
        vm.pnlClass = function (value) { return value > 0 ? 'stock-up' : value < 0 ? 'stock-down' : 'stock-ref'; };
        vm.tradeUrl = function (code, side) { return apiUrls.trade + '?code=' + encodeURIComponent(code) + (side ? '&side=' + side : ''); };
        vm.detailUrl = function (code) { return apiUrls.detail.replace('__CODE__', encodeURIComponent(code)); };
    }

    app.controller('DashboardController', ['$http', '$interval', '$scope', 'apiUrls', 'watchlistApi',
        function ($http, $interval, $scope, apiUrls, watchlistApi) {
            var vm = this;
            var chart = null;
            var cashLabel = document.querySelector('[ng-app]').getAttribute('data-cash-label') || 'Cash';
            decorate(vm, apiUrls);

            vm.loading = true;
            vm.summary = null;
            vm.watchlist = [];

            function renderChart() {
                var canvas = document.getElementById('allocationChart');
                if (!canvas || !window.Chart || !vm.summary) return;

                var labels = vm.summary.holdings.map(function (h) { return h.stockCode; }).concat([cashLabel]);
                var data = vm.summary.holdings.map(function (h) { return h.marketValue; })
                    .concat([vm.summary.cashBalance + vm.summary.blockedCash]);
                var palette = ['--brand-primary', '--stock-up', '--stock-ceil', '--stock-floor', '--stock-ref', '--stock-down']
                    .map(cssVar);
                var colors = labels.map(function (_, i) { return i === labels.length - 1 ? cssVar('--text-muted') : palette[i % palette.length]; });

                if (chart) chart.destroy();
                chart = new Chart(canvas, {
                    type: 'doughnut',
                    data: { labels: labels, datasets: [{ data: data, backgroundColor: colors, borderColor: cssVar('--bg-surface'), borderWidth: 2 }] },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: '62%',
                        plugins: {
                            legend: { position: 'bottom', labels: { color: cssVar('--text-secondary'), boxWidth: 12 } },
                            tooltip: { callbacks: { label: function (ctx) { return ctx.label + ': ' + moneyFormat.format(ctx.parsed); } } }
                        }
                    }
                });
            }

            vm.refresh = function () {
                vm.loading = true;
                $http.get(apiUrls.summary)
                    .then(function (r) { vm.summary = r.data; renderChart(); }, showError)
                    .finally(function () { vm.loading = false; });
                watchlistApi.list().then(function (items) { vm.watchlist = items; }, showError);
            };

            vm.removeFromWatchlist = function (code) {
                watchlistApi.remove(code).then(function () {
                    vm.watchlist = vm.watchlist.filter(function (i) { return i.quote.stockCode !== code; });
                }, showError);
            };

            vm.refresh();
            var timer = $interval(vm.refresh, 30000);
            $scope.$on('$destroy', function () {
                $interval.cancel(timer);
                if (chart) chart.destroy();
            });
        }]);

    app.controller('WatchlistController', ['$http', 'apiUrls', 'watchlistApi',
        function ($http, apiUrls, watchlistApi) {
            var vm = this;
            decorate(vm, apiUrls);

            vm.loading = true;
            vm.items = [];
            vm.stocks = [];
            vm.selectedCode = '';

            function inWatchlist(code) {
                return vm.items.some(function (i) { return i.quote.stockCode === code; });
            }

            vm.availableStocks = function () {
                return vm.stocks.filter(function (s) { return !inWatchlist(s.stockCode); });
            };

            vm.load = function () {
                vm.loading = true;
                watchlistApi.list()
                    .then(function (items) { vm.items = items; }, showError)
                    .finally(function () { vm.loading = false; });
            };

            vm.add = function () {
                if (!vm.selectedCode) return;
                watchlistApi.toggle(vm.selectedCode).then(function () {
                    vm.selectedCode = '';
                    vm.load();
                }, showError);
            };

            vm.remove = function (code) {
                watchlistApi.remove(code).then(function () {
                    vm.items = vm.items.filter(function (i) { return i.quote.stockCode !== code; });
                }, showError);
            };

            // API phân trang (tối đa 100 mã/trang) — đủ cho toàn bộ mã ngân hàng.
            $http.get(apiUrls.stocks, { params: { pageSize: 100 } })
                .then(function (r) { vm.stocks = r.data.items; }, showError);
            vm.load();
        }]);
})(window.angular);

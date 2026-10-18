using BankStock.Tests.Infrastructure;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels.Investor;
using Microsoft.EntityFrameworkCore;
using static BankStock.Tests.Infrastructure.TestDatabase;

namespace BankStock.Tests.Services;

public class OrderServiceTests
{
    private static decimal Fee(decimal value) => TradingRules.CalculateFee(value, FeePercentage);

    private static PlaceOrderCommand Buy(int quantity, decimal? price = null, string stockCode = StockCode) =>
        new(stockCode, OrderType.Buy, price is null ? OrderMode.Market : OrderMode.Limit, quantity, price);

    private static PlaceOrderCommand Sell(int quantity, decimal? price = null) =>
        new(StockCode, OrderType.Sell, price is null ? OrderMode.Market : OrderMode.Limit, quantity, price);

    private static async Task<(ServiceResult Result, OrderPlacedDto? Order)> PlaceAsync(TestDatabase db, PlaceOrderCommand command, string userId = InvestorId)
    {
        await using var context = db.CreateContext();
        return await db.CreateOrderService(context).PlaceOrderAsync(userId, command);
    }

    private static async Task<ServiceResult> CancelAsync(TestDatabase db, int orderId, string userId = InvestorId)
    {
        await using var context = db.CreateContext();
        return await db.CreateOrderService(context).CancelOrderAsync(userId, orderId);
    }

    // ---------- Đặt lệnh mua ----------

    [Fact]
    public async Task PlaceOrder_MarketBuy_MatchesAtMarketPriceAndDebitsValuePlusFee()
    {
        var db = await TestDatabase.CreateAsync();
        var value = 1_000 * MatchPrice;

        var (result, order) = await PlaceAsync(db, Buy(1_000));

        Assert.True(result.Succeeded);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Matched, order.Status);
        Assert.Equal(MatchPrice, order.MatchedPrice);
        Assert.Equal(Fee(value), order.Fee);

        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance - value - Fee(value), wallet.Balance);
        Assert.Equal(0m, wallet.BlockedBalance);

        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.NotNull(holding);
        Assert.Equal(1_000, holding.Quantity);
        Assert.Equal(0, holding.BlockedQuantity);
        Assert.Equal(MatchPrice, holding.AvgCost);
    }

    [Fact]
    public async Task PlaceOrder_MarketBuy_WritesTradeAndCashLedger()
    {
        var db = await TestDatabase.CreateAsync();
        var value = 500 * MatchPrice;

        var (_, order) = await PlaceAsync(db, Buy(500));

        await using var context = db.CreateContext();
        var trade = await context.Trades.SingleAsync();
        Assert.Equal(order!.OrderId, trade.OrderId);
        Assert.Equal(InvestorId, trade.UserId);
        Assert.Equal(500, trade.Quantity);
        Assert.Equal(MatchPrice, trade.Price);
        Assert.Equal(Fee(value), trade.Fee);

        // Hai bút toán: trừ giá trị khớp rồi trừ phí; BalanceAfter của bút toán cuối = tổng tiền sau giao dịch.
        var ledger = await context.WalletTransactions.OrderBy(t => t.Id).ToListAsync();
        Assert.Collection(ledger,
            debit =>
            {
                Assert.Equal(WalletTransactionType.BuyDebit, debit.Type);
                Assert.Equal(-value, debit.Amount);
                Assert.Equal(InitialBalance - value, debit.BalanceAfter);
            },
            fee =>
            {
                Assert.Equal(WalletTransactionType.Fee, fee.Type);
                Assert.Equal(-Fee(value), fee.Amount);
                Assert.Equal(InitialBalance - value - Fee(value), fee.BalanceAfter);
            });
    }

    [Fact]
    public async Task PlaceOrder_LimitBuyBelowMarket_StaysPendingAndBlocksCash()
    {
        var db = await TestDatabase.CreateAsync();
        const decimal limit = 85_000m;
        var blocked = 1_000 * limit + Fee(1_000 * limit);

        var (result, order) = await PlaceAsync(db, Buy(1_000, limit));

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Pending, order!.Status);
        Assert.Null(order.MatchedPrice);

        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance - blocked, wallet.Balance);
        Assert.Equal(blocked, wallet.BlockedBalance);

        await using var context = db.CreateContext();
        var saved = await context.Orders.SingleAsync();
        Assert.Equal(blocked, saved.BlockedAmount);
        Assert.Empty(context.Trades);
        Assert.Empty(context.WalletTransactions); // giam tiền không làm đổi tổng tiền nên không ghi sổ
        Assert.Null(await db.GetHoldingAsync(InvestorId));
    }

    [Fact]
    public async Task PlaceOrder_LimitBuyAboveMarket_MatchesImmediatelyAtMarketPriceAndReleasesExcess()
    {
        var db = await TestDatabase.CreateAsync();
        const decimal limit = 95_000m;
        var value = 1_000 * MatchPrice;

        var (_, order) = await PlaceAsync(db, Buy(1_000, limit));

        Assert.Equal(OrderStatus.Matched, order!.Status);
        Assert.Equal(MatchPrice, order.MatchedPrice);

        // Người mua chỉ chịu giá khớp thị trường, phần giam theo giá đặt cao hơn được trả lại.
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance - value - Fee(value), wallet.Balance);
        Assert.Equal(0m, wallet.BlockedBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(-100)]
    [InlineData(TradingRules.MaxOrderQuantity + TradingRules.LotSize)]
    public async Task PlaceOrder_QuantityNotValidLot_ReturnsValidationError(int quantity)
    {
        var db = await TestDatabase.CreateAsync();

        var (result, order) = await PlaceAsync(db, Buy(quantity));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("OrderQuantityInvalid", result.ErrorKey);
        Assert.Equal("Quantity", result.Field);
        Assert.Null(order);
        await using var context = db.CreateContext();
        Assert.Empty(context.Orders);
    }

    [Theory]
    [InlineData(83_600)] // dưới giá sàn
    [InlineData(96_400)] // trên giá trần
    [InlineData(0)]
    public async Task PlaceOrder_LimitPriceOutsideBand_ReturnsValidationError(int price)
    {
        var db = await TestDatabase.CreateAsync();

        var (result, _) = await PlaceAsync(db, Buy(100, price));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("OrderPriceOutOfBand", result.ErrorKey);
        Assert.Equal([FloorPrice, CeilingPrice], result.ErrorArgs);
    }

    [Theory]
    [InlineData(83_700)]
    [InlineData(96_300)]
    public async Task PlaceOrder_LimitPriceOnFloorOrCeiling_IsAccepted(int price)
    {
        var db = await TestDatabase.CreateAsync();

        var (result, _) = await PlaceAsync(db, Buy(100, price));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task PlaceOrder_BuyMoreThanAvailableCash_IsRejectedAndWalletUnchanged()
    {
        var db = await TestDatabase.CreateAsync();
        // 500.000 cp × 90.000 = 45 tỷ, vượt xa 500 triệu.
        var (result, order) = await PlaceAsync(db, Buy(TradingRules.MaxOrderQuantity));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("OrderInsufficientCash", result.ErrorKey);
        Assert.Null(order);

        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance, wallet.Balance);
        Assert.Equal(0m, wallet.BlockedBalance);
        await using var context = db.CreateContext();
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task PlaceOrder_BuyNeedsCashForValueAndFee_RejectsWhenOnlyValueIsCovered()
    {
        var db = await TestDatabase.CreateAsync();
        var value = 1_000 * MatchPrice;
        await db.UpdateWalletAsync(InvestorId, w => w.Balance = value); // đủ giá trị lệnh nhưng thiếu tiền phí

        var (result, _) = await PlaceAsync(db, Buy(1_000));

        Assert.Equal("OrderInsufficientCash", result.ErrorKey);
    }

    [Fact]
    public async Task PlaceOrder_BlockedCashCannotBeReused()
    {
        var db = await TestDatabase.CreateAsync();
        // Lệnh chờ đầu tiên giam phần lớn số dư (5.000 cp × 85.000 ≈ 425,6 triệu).
        var (first, _) = await PlaceAsync(db, Buy(5_000, 85_000m));
        Assert.True(first.Succeeded);

        var (second, _) = await PlaceAsync(db, Buy(1_000, 85_000m));

        Assert.Equal("OrderInsufficientCash", second.ErrorKey);
    }

    [Theory]
    [InlineData("XYZ")]          // mã không tồn tại
    [InlineData(InactiveStockCode)] // mã ngừng giao dịch
    public async Task PlaceOrder_StockNotTradable_ReturnsValidationError(string stockCode)
    {
        var db = await TestDatabase.CreateAsync();

        var (result, _) = await PlaceAsync(db, Buy(100, stockCode: stockCode));

        Assert.Equal("OrderStockNotTradable", result.ErrorKey);
        Assert.Equal("StockCode", result.Field);
    }

    [Fact]
    public async Task PlaceOrder_StockCodeIsNormalized()
    {
        var db = await TestDatabase.CreateAsync();

        var (result, order) = await PlaceAsync(db, Buy(100, stockCode: " vcb "));

        Assert.True(result.Succeeded);
        Assert.Equal(StockCode, order!.StockCode);
    }

    [Fact]
    public async Task PlaceOrder_InactiveWallet_ReturnsConflict()
    {
        var db = await TestDatabase.CreateAsync();
        await db.UpdateWalletAsync(InvestorId, w => w.IsActive = false);

        var (result, _) = await PlaceAsync(db, Buy(100));

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("WalletInactive", result.ErrorKey);
    }

    [Fact]
    public async Task PlaceOrder_SecondBuy_UpdatesWeightedAverageCost()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 1_000, 80_000m);

        await PlaceAsync(db, Buy(1_000));

        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(2_000, holding!.Quantity);
        // (1.000 × 80.000 + 1.000 × 90.000) / 2.000 = 85.000 (không tính phí).
        Assert.Equal(85_000m, holding.AvgCost);
    }

    [Fact]
    public async Task PlaceOrder_UsesConfiguredFeeRoundedToWholeDong()
    {
        var db = await TestDatabase.CreateAsync();

        var (_, order) = await PlaceAsync(db, Buy(100, 83_700m)); // 8.370.000 × 0,15% = 12.555

        Assert.Equal(12_555m, order!.Fee);
    }

    // ---------- Đặt lệnh bán ----------

    [Fact]
    public async Task PlaceOrder_SellWithoutHolding_IsRejected()
    {
        var db = await TestDatabase.CreateAsync();

        var (result, _) = await PlaceAsync(db, Sell(100));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("OrderInsufficientShares", result.ErrorKey);
    }

    [Fact]
    public async Task PlaceOrder_SellMoreThanHolding_IsRejectedAndHoldingUnchanged()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 300, 80_000m);

        var (result, _) = await PlaceAsync(db, Sell(400));

        Assert.Equal("OrderInsufficientShares", result.ErrorKey);
        Assert.Equal([300, StockCode], result.ErrorArgs);
        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(300, holding!.Quantity);
        Assert.Equal(0, holding.BlockedQuantity);
    }

    [Fact]
    public async Task PlaceOrder_MarketSellPartOfHolding_CreditsValueMinusFee()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 1_000, 80_000m);
        var value = 400 * MatchPrice;

        var (result, order) = await PlaceAsync(db, Sell(400));

        Assert.True(result.Succeeded);
        Assert.Equal(OrderStatus.Matched, order!.Status);
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance + value - Fee(value), wallet.Balance);

        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(600, holding!.Quantity);
        Assert.Equal(0, holding.BlockedQuantity);
        Assert.Equal(80_000m, holding.AvgCost); // bán không làm đổi giá vốn

        await using var context = db.CreateContext();
        Assert.Contains(context.WalletTransactions, t => t.Type == WalletTransactionType.SellCredit && t.Amount == value);
    }

    [Fact]
    public async Task PlaceOrder_SellWholeHolding_RemovesHoldingRow()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 500, 80_000m);

        await PlaceAsync(db, Sell(500));

        Assert.Null(await db.GetHoldingAsync(InvestorId));
    }

    [Fact]
    public async Task PlaceOrder_LimitSellAboveMarket_StaysPendingAndBlocksShares()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 1_000, 80_000m);

        var (_, order) = await PlaceAsync(db, Sell(600, 95_000m));

        Assert.Equal(OrderStatus.Pending, order!.Status);
        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(400, holding!.Quantity);
        Assert.Equal(600, holding.BlockedQuantity);
        Assert.Equal(InitialBalance, (await db.GetWalletAsync(InvestorId)).Balance);
    }

    // ---------- Hủy lệnh ----------

    [Fact]
    public async Task CancelOrder_PendingBuy_RefundsBlockedCash()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, order) = await PlaceAsync(db, Buy(1_000, 85_000m));

        var result = await CancelAsync(db, order!.OrderId);

        Assert.True(result.Succeeded);
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance, wallet.Balance);
        Assert.Equal(0m, wallet.BlockedBalance);

        await using var context = db.CreateContext();
        var saved = await context.Orders.SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        Assert.NotNull(saved.UpdatedAt);
        Assert.Contains(context.AuditLogs, l => l.Action == "CancelOrder" && l.EntityId == order.OrderId.ToString());
    }

    [Fact]
    public async Task CancelOrder_PendingSell_ReturnsBlockedShares()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 1_000, 80_000m);
        var (_, order) = await PlaceAsync(db, Sell(600, 95_000m));

        var result = await CancelAsync(db, order!.OrderId);

        Assert.True(result.Succeeded);
        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(1_000, holding!.Quantity);
        Assert.Equal(0, holding.BlockedQuantity);
    }

    [Fact]
    public async Task CancelOrder_OfAnotherInvestor_ReturnsNotFoundAndKeepsOrder()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, order) = await PlaceAsync(db, Buy(1_000, 85_000m), OtherInvestorId);

        var result = await CancelAsync(db, order!.OrderId, InvestorId);

        // Chống IDOR: lệnh của người khác được xử lý như không tồn tại.
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        await using var context = db.CreateContext();
        Assert.Equal(OrderStatus.Pending, (await context.Orders.SingleAsync()).Status);
        Assert.Equal(order.Wallet.BlockedBalance, (await db.GetWalletAsync(OtherInvestorId)).BlockedBalance);
    }

    [Fact]
    public async Task CancelOrder_AlreadyMatched_ReturnsConflict()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, order) = await PlaceAsync(db, Buy(100));

        var result = await CancelAsync(db, order!.OrderId);

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("OrderNotPending", result.ErrorKey);
    }

    [Fact]
    public async Task CancelOrder_Twice_SecondCallReturnsConflictAndDoesNotRefundAgain()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, order) = await PlaceAsync(db, Buy(1_000, 85_000m));
        await CancelAsync(db, order!.OrderId);

        var second = await CancelAsync(db, order.OrderId);

        Assert.Equal("OrderNotPending", second.ErrorKey);
        Assert.Equal(InitialBalance, (await db.GetWalletAsync(InvestorId)).Balance);
    }

    // ---------- Khớp lệnh chờ khi Admin đổi giá ----------

    [Fact]
    public async Task MatchPendingOrders_MatchesOnlyOrdersSatisfiedByNewPrice()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(OtherInvestorId, 1_000, 80_000m);
        var (_, buyAt88) = await PlaceAsync(db, Buy(1_000, 88_000m));
        var (_, buyAt85) = await PlaceAsync(db, Buy(1_000, 85_000m));
        var (_, sellAt95) = await PlaceAsync(db, Sell(500, 95_000m), OtherInvestorId);

        int matched;
        await using (var context = db.CreateContext())
        {
            matched = await db.CreateOrderService(context).MatchPendingOrdersAsync(db.StockId, 87_000m);
        }

        Assert.Equal(1, matched);
        await using var verify = db.CreateContext();
        var statuses = await verify.Orders.ToDictionaryAsync(o => o.Id, o => o.Status);
        Assert.Equal(OrderStatus.Matched, statuses[buyAt88!.OrderId]);
        Assert.Equal(OrderStatus.Pending, statuses[buyAt85!.OrderId]);
        Assert.Equal(OrderStatus.Pending, statuses[sellAt95!.OrderId]);
        Assert.Equal(87_000m, (await verify.Trades.SingleAsync()).Price);
    }

    [Fact]
    public async Task MatchPendingOrders_BuyFilledBelowLimit_ReleasesBlockedCashAndKeepsOtherPendingBlock()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, matchedOrder) = await PlaceAsync(db, Buy(1_000, 88_000m));
        var (_, pendingOrder) = await PlaceAsync(db, Buy(1_000, 85_000m));

        await using (var context = db.CreateContext())
        {
            await db.CreateOrderService(context).MatchPendingOrdersAsync(db.StockId, 87_000m);
        }

        var value = 1_000 * 87_000m;
        var stillBlocked = 1_000 * 85_000m + Fee(1_000 * 85_000m);
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(stillBlocked, wallet.BlockedBalance);
        Assert.Equal(InitialBalance - value - Fee(value) - stillBlocked, wallet.Balance);

        var holding = await db.GetHoldingAsync(InvestorId);
        Assert.Equal(1_000, holding!.Quantity);
        Assert.Equal(87_000m, holding.AvgCost);
        Assert.NotEqual(matchedOrder!.OrderId, pendingOrder!.OrderId);
    }

    [Fact]
    public async Task MatchPendingOrders_SellWhenPriceRises_CreditsSellerAndClearsBlockedShares()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 500, 80_000m);
        await PlaceAsync(db, Sell(500, 92_000m));

        int matched;
        await using (var context = db.CreateContext())
        {
            matched = await db.CreateOrderService(context).MatchPendingOrdersAsync(db.StockId, 93_000m);
        }

        Assert.Equal(1, matched);
        var value = 500 * 93_000m;
        Assert.Equal(InitialBalance + value - Fee(value), (await db.GetWalletAsync(InvestorId)).Balance);
        Assert.Null(await db.GetHoldingAsync(InvestorId));
    }

    [Fact]
    public async Task MatchPendingOrders_IgnoresCancelledOrders()
    {
        var db = await TestDatabase.CreateAsync();
        var (_, order) = await PlaceAsync(db, Buy(1_000, 88_000m));
        await CancelAsync(db, order!.OrderId);

        await using var context = db.CreateContext();
        var matched = await db.CreateOrderService(context).MatchPendingOrdersAsync(db.StockId, 87_000m);

        Assert.Equal(0, matched);
        Assert.Empty(context.Trades);
    }

    [Fact]
    public async Task MatchPendingOrders_UnknownStock_ReturnsZero()
    {
        var db = await TestDatabase.CreateAsync();
        await using var context = db.CreateContext();

        Assert.Equal(0, await db.CreateOrderService(context).MatchPendingOrdersAsync(9_999, 87_000m));
    }

    // ---------- Truy vấn ----------

    [Fact]
    public async Task GetOrders_ReturnsOnlyOrdersOfRequestingInvestor()
    {
        var db = await TestDatabase.CreateAsync();
        await PlaceAsync(db, Buy(100, 85_000m));
        await PlaceAsync(db, Buy(200, 85_000m));
        var (_, foreign) = await PlaceAsync(db, Buy(300, 85_000m), OtherInvestorId);

        await using var context = db.CreateContext();
        var service = db.CreateOrderService(context);
        var list = await service.GetOrdersAsync(InvestorId, new OrderListQuery());

        Assert.Equal(2, list.TotalCount);
        Assert.All(list.Items, o => Assert.NotEqual(foreign!.OrderId, o.Id));
        Assert.Equal([200, 100], list.Items.Select(o => o.Quantity)); // mới nhất trước
        Assert.Null(await service.GetOrderAsync(InvestorId, foreign!.OrderId));
    }

    [Fact]
    public async Task GetOrders_FiltersByStatus()
    {
        var db = await TestDatabase.CreateAsync();
        await PlaceAsync(db, Buy(100));             // khớp ngay
        await PlaceAsync(db, Buy(100, 85_000m));    // chờ

        await using var context = db.CreateContext();
        var list = await db.CreateOrderService(context)
            .GetOrdersAsync(InvestorId, new OrderListQuery { Status = OrderStatus.Pending });

        var only = Assert.Single(list.Items);
        Assert.Equal(OrderStatus.Pending, only.Status);
        Assert.True(only.CanCancel);
    }

    [Fact]
    public async Task GetQuote_ReturnsLatestSessionAndAvailableBalances()
    {
        var db = await TestDatabase.CreateAsync();
        await db.AddHoldingAsync(InvestorId, 700, 80_000m);

        await using var context = db.CreateContext();
        var quote = await db.CreateOrderService(context).GetQuoteAsync(InvestorId, StockCode);

        Assert.NotNull(quote);
        Assert.Equal(MatchPrice, quote.MatchPrice);
        Assert.Equal(FloorPrice, quote.FloorPrice);
        Assert.Equal(CeilingPrice, quote.CeilingPrice);
        Assert.Equal(InitialBalance, quote.AvailableCash);
        Assert.Equal(700, quote.AvailableQuantity);
    }
}

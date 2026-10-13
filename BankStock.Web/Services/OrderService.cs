using System.Globalization;
using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext context, IAuditLogService auditLog, IConfiguration configuration, ILogger<OrderService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Phí giao dịch (%) cấu hình trong appsettings "TradingFeePercentage".</summary>
    private decimal FeePercentage => _configuration.GetValue("TradingFeePercentage", TradingRules.DefaultFeePercentage);

    public Task<List<StockOptionItem>> GetTradableStocksAsync(CancellationToken cancellationToken = default) =>
        _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.PriceHistories.Any())
            .OrderBy(s => s.StockCode)
            .Select(s => new StockOptionItem(s.Id, s.StockCode, s.Exchange, s.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<TradeQuoteDto?> GetQuoteAsync(string userId, string stockCode, CancellationToken cancellationToken = default)
    {
        var market = await GetMarketAsync(stockCode, cancellationToken);
        if (market is null)
        {
            return null;
        }

        var cash = await _context.Wallets.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => (decimal?)w.Balance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;
        var shares = await _context.Holdings.AsNoTracking()
            .Where(h => h.UserId == userId && h.StockId == market.StockId)
            .Select(h => (int?)h.Quantity)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        return new TradeQuoteDto(market.StockCode, market.BankShortName, market.Exchange, market.TradeDate,
            market.RefPrice, market.CeilingPrice, market.FloorPrice, market.MatchPrice,
            cash, shares, FeePercentage, TradingRules.LotSize);
    }

    public async Task<(ServiceResult Result, OrderPlacedDto? Order)> PlaceOrderAsync(string userId, PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity < TradingRules.LotSize || command.Quantity > TradingRules.MaxOrderQuantity || command.Quantity % TradingRules.LotSize != 0)
        {
            return (ServiceResult.Invalid("OrderQuantityInvalid", "Quantity", TradingRules.LotSize, TradingRules.MaxOrderQuantity), null);
        }

        var market = await GetMarketAsync(command.StockCode, cancellationToken);
        if (market is null)
        {
            return (ServiceResult.Invalid("OrderStockNotTradable", "StockCode", command.StockCode), null);
        }

        var price = command.Mode == OrderMode.Market ? market.MatchPrice : command.Price ?? 0m;
        if (price < market.FloorPrice || price > market.CeilingPrice)
        {
            return (ServiceResult.Invalid("OrderPriceOutOfBand", "Price", market.FloorPrice, market.CeilingPrice), null);
        }

        // Lệnh Limit đã "chạm" giá thị trường thì khớp ngay theo giá khớp hiện tại (có lợi hơn hoặc bằng giá đặt).
        bool matchNow = command.Mode == OrderMode.Market
            || (command.Type == OrderType.Buy ? market.MatchPrice <= price : market.MatchPrice >= price);
        var feePercentage = FeePercentage;

        (ServiceResult Result, OrderPlacedDto? Order) Fail(ServiceResult result) => (result, null);

        var outcome = await ConcurrencyRetry.ExecuteAsync<(ServiceResult Result, OrderPlacedDto? Order)>(_context, _logger, $"PlaceOrder {market.StockCode} by {userId}", async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
            if (wallet is null)
            {
                return Fail(ServiceResult.Conflict("WalletNotFound"));
            }
            if (!wallet.IsActive)
            {
                return Fail(ServiceResult.Conflict("WalletInactive"));
            }
            var holding = await _context.Holdings.FirstOrDefaultAsync(h => h.UserId == userId && h.StockId == market.StockId, cancellationToken);

            var order = new Order
            {
                UserId = userId,
                StockId = market.StockId,
                Type = command.Type,
                Mode = command.Mode,
                Status = OrderStatus.Pending,
                Quantity = command.Quantity,
                Price = price,
                CreatedAt = DateTime.UtcNow
            };

            var estimatedFee = TradingRules.CalculateFee(command.Quantity * price, feePercentage);
            if (command.Type == OrderType.Buy)
            {
                // Giam tiền = giá trị lệnh + phí dự kiến.
                var blocked = command.Quantity * price + estimatedFee;
                if (wallet.Balance < blocked)
                {
                    return Fail(ServiceResult.Invalid("OrderInsufficientCash", null, blocked, wallet.Balance));
                }
                wallet.Balance -= blocked;
                wallet.BlockedBalance += blocked;
                order.BlockedAmount = blocked;
            }
            else
            {
                var available = holding?.Quantity ?? 0;
                if (holding is null || available < command.Quantity)
                {
                    return Fail(ServiceResult.Invalid("OrderInsufficientShares", null, available, market.StockCode));
                }
                holding.Quantity -= command.Quantity;
                holding.BlockedQuantity += command.Quantity;
            }

            order.Fee = estimatedFee;
            wallet.LastUpdatedAt = DateTime.UtcNow;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken); // cần Order.Id cho Trade

            decimal? matchedPrice = null;
            if (matchNow)
            {
                ApplyMatch(order, wallet, holding, market.StockCode, market.MatchPrice, feePercentage);
                await _context.SaveChangesAsync(cancellationToken);
                matchedPrice = market.MatchPrice;
            }

            await transaction.CommitAsync(cancellationToken);

            var placed = new OrderPlacedDto(order.Id, market.StockCode, order.Type, order.Mode, order.Status, order.Quantity,
                order.Price, matchedPrice, order.Fee, new WalletSummaryDto(wallet.Balance, wallet.BlockedBalance, wallet.IsActive));
            return (ServiceResult.Success(order.Id), (OrderPlacedDto?)placed);
        }, () => Fail(ServiceResult.Conflict("ConcurrencyConflict")));

        if (outcome.Order is { } result)
        {
            await _auditLog.LogAsync("PlaceOrder", userId, nameof(Order), result.OrderId.ToString(), Describe(result), cancellationToken);
            _logger.LogInformation("Order {OrderId} placed by {UserId}: {Detail}", result.OrderId, userId, Describe(result));
        }
        return outcome;
    }

    public async Task<ServiceResult> CancelOrderAsync(string userId, int orderId, CancellationToken cancellationToken = default)
    {
        var result = await ConcurrencyRetry.ExecuteAsync(_context, _logger, $"CancelOrder {orderId} by {userId}", async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Lọc theo UserId: lệnh của người khác coi như không tồn tại (chống IDOR).
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
            if (order is null)
            {
                return ServiceResult.NotFound();
            }
            if (order.Status != OrderStatus.Pending)
            {
                return ServiceResult.Conflict("OrderNotPending");
            }

            if (order.Type == OrderType.Buy)
            {
                var wallet = await _context.Wallets.FirstAsync(w => w.UserId == userId, cancellationToken);
                wallet.BlockedBalance -= order.BlockedAmount;
                wallet.Balance += order.BlockedAmount;
                wallet.LastUpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var holding = await _context.Holdings.FirstAsync(h => h.UserId == userId && h.StockId == order.StockId, cancellationToken);
                holding.BlockedQuantity -= order.Quantity;
                holding.Quantity += order.Quantity;
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ServiceResult.Success(order.Id);
        }, () => ServiceResult.Conflict("ConcurrencyConflict"));

        if (result.Succeeded)
        {
            await _auditLog.LogAsync("CancelOrder", userId, nameof(Order), orderId.ToString(), null, cancellationToken);
        }
        return result;
    }

    public async Task<int> MatchPendingOrdersAsync(int stockId, decimal matchPrice, CancellationToken cancellationToken = default)
    {
        var stockCode = await _context.Stocks.Where(s => s.Id == stockId).Select(s => s.StockCode).FirstOrDefaultAsync(cancellationToken);
        if (stockCode is null)
        {
            return 0;
        }

        var candidateIds = await _context.Orders
            .AsNoTracking()
            .Where(o => o.StockId == stockId && o.Status == OrderStatus.Pending && o.Mode == OrderMode.Limit
                && ((o.Type == OrderType.Buy && o.Price >= matchPrice) || (o.Type == OrderType.Sell && o.Price <= matchPrice)))
            .OrderBy(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var feePercentage = FeePercentage;
        var matched = 0;
        foreach (var orderId in candidateIds)
        {
            var owner = await ConcurrencyRetry.ExecuteAsync(_context, _logger, $"MatchOrder {orderId}", async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                // Đọc lại: lệnh có thể vừa bị hủy trong lúc chờ.
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
                if (order is null || order.Status != OrderStatus.Pending)
                {
                    return null;
                }

                var wallet = await _context.Wallets.FirstAsync(w => w.UserId == order.UserId, cancellationToken);
                var holding = await _context.Holdings.FirstOrDefaultAsync(h => h.UserId == order.UserId && h.StockId == stockId, cancellationToken);
                if (order.Type == OrderType.Sell && holding is null)
                {
                    _logger.LogError("Pending sell order {OrderId} has no holding row, skipped", orderId);
                    return null;
                }

                ApplyMatch(order, wallet, holding, stockCode, matchPrice, feePercentage);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return order.UserId;
            }, () => (string?)null);

            if (owner is not null)
            {
                matched++;
                await _auditLog.LogAsync("OrderMatched", owner, nameof(Order), orderId.ToString(),
                    string.Create(CultureInfo.InvariantCulture, $"{stockCode} @ {matchPrice:0}"), cancellationToken);
            }
        }

        if (matched > 0)
        {
            _logger.LogInformation("Matched {Count} pending orders of {StockCode} at {Price}", matched, stockCode, matchPrice);
        }
        return matched;
    }

    public async Task<PaginatedList<OrderListItem>> GetOrdersAsync(string userId, OrderListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var orders = _context.Orders.AsNoTracking().Where(o => o.UserId == userId);

        if (query.StockId is { } stockId) orders = orders.Where(o => o.StockId == stockId);
        if (query.Status is { } status) orders = orders.Where(o => o.Status == status);
        if (query.Type is { } type) orders = orders.Where(o => o.Type == type);
        if (query.Q is { } keyword) orders = orders.Where(o => o.Stock!.StockCode.Contains(keyword));

        return await ProjectOrders(orders
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<OrderListItem?> GetOrderAsync(string userId, int orderId, CancellationToken cancellationToken = default) =>
        ProjectOrders(_context.Orders.AsNoTracking().Where(o => o.UserId == userId && o.Id == orderId))
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Giá khớp lấy từ Trade của lệnh (mỗi lệnh khớp toàn bộ một lần).</summary>
    private IQueryable<OrderListItem> ProjectOrders(IQueryable<Order> orders) =>
        orders.Select(o => new OrderListItem(
            o.Id, o.Stock!.StockCode, o.Type, o.Mode, o.Status, o.Quantity, o.Price,
            _context.Trades.Where(t => t.OrderId == o.Id).Select(t => (decimal?)t.Price).FirstOrDefault(),
            o.Fee, o.CreatedAt, o.UpdatedAt));

    public async Task<PaginatedList<TradeListItem>> GetTradesAsync(string userId, TradeListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var trades = _context.Trades.AsNoTracking().Where(t => t.UserId == userId);

        if (query.StockId is { } stockId) trades = trades.Where(t => t.StockId == stockId);
        if (query.Type is { } type) trades = trades.Where(t => t.Type == type);
        if (query.From is { } from)
        {
            var fromDate = from.Date;
            trades = trades.Where(t => t.CreatedAt >= fromDate);
        }
        if (query.To is { } to)
        {
            var toExclusive = to.Date.AddDays(1);
            trades = trades.Where(t => t.CreatedAt < toExclusive);
        }

        return await trades
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(t => new TradeListItem(t.Id, t.OrderId, t.Stock!.StockCode, t.Type, t.Quantity, t.Price, t.Fee, t.CreatedAt))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    /// <summary>
    /// Khớp một lệnh (chưa SaveChanges): giải phóng phần giam, trừ/cộng tiền theo giá khớp thực tế, cập nhật danh mục,
    /// ghi Trade và WalletTransaction (BalanceAfter = tổng tiền khả dụng + phong tỏa sau giao dịch).
    /// </summary>
    private void ApplyMatch(Order order, Wallet wallet, Holding? holding, string stockCode, decimal executionPrice, decimal feePercentage)
    {
        var now = DateTime.UtcNow;
        var value = order.Quantity * executionPrice;
        var fee = TradingRules.CalculateFee(value, feePercentage);
        var description = string.Create(CultureInfo.InvariantCulture,
            $"{(order.Type == OrderType.Buy ? "Mua" : "Bán")} {order.Quantity} {stockCode} @ {executionPrice:0} (lệnh #{order.Id})");

        if (order.Type == OrderType.Buy)
        {
            // Giá khớp ≤ giá đặt nên phần giam luôn đủ; phần thừa trả lại số dư khả dụng.
            wallet.BlockedBalance -= order.BlockedAmount;
            wallet.Balance += order.BlockedAmount - value - fee;

            if (holding is null)
            {
                holding = new Holding { UserId = order.UserId, StockId = order.StockId };
                _context.Holdings.Add(holding);
            }

            // Giá vốn bình quân gia quyền (không tính phí): ((Qty cũ × AvgCost cũ) + (Qty mua × Giá mua)) / (Qty cũ + Qty mua).
            var held = holding.Quantity + holding.BlockedQuantity;
            holding.AvgCost = Math.Round((held * holding.AvgCost + value) / (held + order.Quantity), 2);
            holding.Quantity += order.Quantity;
            AddCashTransactions(wallet, WalletTransactionType.BuyDebit, -value, fee, description, now);
        }
        else
        {
            holding!.BlockedQuantity -= order.Quantity;
            wallet.Balance += value - fee;
            if (holding.Quantity + holding.BlockedQuantity == 0)
            {
                _context.Holdings.Remove(holding);
            }
            AddCashTransactions(wallet, WalletTransactionType.SellCredit, value, fee, description, now);
        }

        order.Status = OrderStatus.Matched;
        order.Fee = fee;
        order.UpdatedAt = now;
        wallet.LastUpdatedAt = now;

        _context.Trades.Add(new Trade
        {
            OrderId = order.Id,
            UserId = order.UserId,
            StockId = order.StockId,
            Type = order.Type,
            Quantity = order.Quantity,
            Price = executionPrice,
            Fee = fee,
            CreatedAt = now
        });
    }

    private void AddCashTransactions(Wallet wallet, WalletTransactionType type, decimal signedValue, decimal fee, string description, DateTime now)
    {
        var totalAfter = wallet.Balance + wallet.BlockedBalance;
        _context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Type = type,
            Amount = signedValue,
            BalanceAfter = totalAfter + fee,
            Description = description,
            CreatedAt = now
        });

        if (fee > 0)
        {
            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = WalletTransactionType.Fee,
                Amount = -fee,
                BalanceAfter = totalAfter,
                Description = description,
                CreatedAt = now
            });
        }
    }

    private async Task<MarketSnapshot?> GetMarketAsync(string stockCode, CancellationToken cancellationToken)
    {
        var code = stockCode.Trim().ToUpperInvariant();
        var row = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.StockCode == code && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.StockCode,
                BankShortName = s.Bank != null ? s.Bank.ShortName : string.Empty,
                s.Exchange,
                Latest = s.PriceHistories
                    .OrderByDescending(p => p.TradeDate)
                    .ThenByDescending(p => p.Id)
                    .Select(p => new { p.TradeDate, p.RefPrice, p.CeilingPrice, p.FloorPrice, p.Close })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row?.Latest is null)
        {
            return null;
        }

        return new MarketSnapshot(row.Id, row.StockCode, row.BankShortName, row.Exchange, row.Latest.TradeDate,
            row.Latest.RefPrice, row.Latest.CeilingPrice, row.Latest.FloorPrice, row.Latest.Close);
    }

    private static string Describe(OrderPlacedDto order) => string.Create(CultureInfo.InvariantCulture,
        $"{order.Type} {order.Mode} {order.Quantity} {order.StockCode} @ {order.Price:0} -> {order.Status}{(order.MatchedPrice is { } p ? $" @ {p:0}" : "")}");

    private sealed record MarketSnapshot(
        int StockId,
        string StockCode,
        string BankShortName,
        string Exchange,
        DateTime TradeDate,
        decimal RefPrice,
        decimal CeilingPrice,
        decimal FloorPrice,
        decimal MatchPrice);
}

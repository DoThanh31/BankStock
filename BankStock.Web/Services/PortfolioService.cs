using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class PortfolioService : IPortfolioService
{
    private static readonly WalletTransactionType[] CapitalTypes =
        [WalletTransactionType.InitialGrant, WalletTransactionType.Deposit, WalletTransactionType.Withdraw];

    private readonly AppDbContext _context;

    public PortfolioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioSummaryDto?> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new { w.Id, w.Balance, w.BlockedBalance })
            .FirstOrDefaultAsync(cancellationToken);
        if (wallet is null)
        {
            return null;
        }

        // Vốn ròng = cấp ban đầu + nạp − rút (Withdraw lưu số âm).
        var netInvested = await _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletId == wallet.Id && CapitalTypes.Contains(t.Type))
            .SumAsync(t => t.Amount, cancellationToken);

        var rows = await _context.Holdings
            .AsNoTracking()
            .Where(h => h.UserId == userId && h.Quantity + h.BlockedQuantity > 0)
            .Select(h => new
            {
                h.Stock!.StockCode,
                BankShortName = h.Stock.Bank != null ? h.Stock.Bank.ShortName : string.Empty,
                h.Quantity,
                h.BlockedQuantity,
                h.AvgCost,
                Latest = h.Stock.PriceHistories
                    .OrderByDescending(p => p.TradeDate)
                    .ThenByDescending(p => p.Id)
                    .Select(p => new { p.Close, p.RefPrice })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var holdings = rows
            .Select(h =>
            {
                var price = h.Latest?.Close ?? h.AvgCost;
                var refPrice = h.Latest?.RefPrice ?? 0m;
                var change = refPrice > 0 ? Math.Round((price - refPrice) / refPrice * 100, 2) : 0m;
                return new HoldingDto(h.StockCode, h.BankShortName, h.Quantity, h.BlockedQuantity, h.AvgCost, price, change);
            })
            .OrderByDescending(h => h.MarketValue)
            .ToList();

        var pendingOrders = await _context.Orders.CountAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending, cancellationToken);

        return new PortfolioSummaryDto(wallet.Balance, wallet.BlockedBalance, netInvested, pendingOrders, holdings);
    }
}

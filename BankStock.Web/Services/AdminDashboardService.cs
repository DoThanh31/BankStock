using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private const int TopStockCount = 5;
    private const int RecentUpdateCount = 6;
    private const int RecentAuditCount = 8;

    private readonly AppDbContext _context;
    private readonly IWalletAdminService _walletService;
    private readonly IAuditLogService _auditLog;

    public AdminDashboardService(AppDbContext context, IWalletAdminService walletService, IAuditLogService auditLog)
    {
        _context = context;
        _walletService = walletService;
        _auditLog = auditLog;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var model = new AdminDashboardViewModel
        {
            TotalBanks = await _context.Banks.CountAsync(cancellationToken),
            TotalStocks = await _context.Stocks.CountAsync(s => s.IsActive, cancellationToken),
            TotalInvestors = await _context.UserRoles.CountAsync(
                ur => _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Investor), cancellationToken)
        };

        await FillLatestSessionAsync(model, cancellationToken);
        await FillTopVolumeAsync(model, cancellationToken);
        await FillInternalTradingAsync(model, cancellationToken);

        model.WalletStats = await _walletService.GetStatsAsync(cancellationToken);
        model.RecentAuditLogs = await _auditLog.GetRecentAsync(RecentAuditCount, cancellationToken);
        return model;
    }

    /// <summary>Phiên gần nhất của từng mã: tổng KL phiên và bảng cập nhật giá mới nhất.</summary>
    private async Task FillLatestSessionAsync(AdminDashboardViewModel model, CancellationToken cancellationToken)
    {
        var latestPrices = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new
            {
                s.StockCode,
                BankShortName = s.Bank != null ? s.Bank.ShortName : string.Empty,
                Latest = s.PriceHistories
                    .OrderByDescending(p => p.TradeDate)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        model.TotalSessionVolume = latestPrices.Sum(p => p.Latest?.Volume ?? 0);
        model.LastSessionDate = latestPrices.Max(p => p.Latest?.TradeDate);
        model.RecentUpdates = latestPrices
            .Where(p => p.Latest != null)
            .OrderByDescending(p => p.Latest!.TradeDate)
            .ThenBy(p => p.StockCode)
            .Take(RecentUpdateCount)
            .Select(p => ToRecentUpdate(p.StockCode, p.BankShortName, p.Latest!))
            .ToList();
    }

    /// <summary>Xếp hạng mã có tổng khối lượng lớn nhất trong N ngày tính tới phiên gần nhất.</summary>
    private async Task FillTopVolumeAsync(AdminDashboardViewModel model, CancellationToken cancellationToken)
    {
        if (model.LastSessionDate is not { } lastDate)
        {
            return;
        }

        var since = lastDate.AddDays(-AdminDashboardViewModel.TopVolumePeriodDays);
        var period = _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.TradeDate > since && p.Stock!.IsActive);

        var perStock = await period
            .GroupBy(p => p.StockId)
            .Select(g => new
            {
                StockId = g.Key,
                Volume = g.Sum(p => p.Volume),
                Value = g.Sum(p => p.Volume * p.Close)
            })
            .OrderByDescending(x => x.Volume)
            .ToListAsync(cancellationToken);

        model.PeriodTotalVolume = perStock.Sum(x => x.Volume);
        model.PeriodTotalValue = perStock.Sum(x => x.Value);

        var top = perStock.Take(TopStockCount).ToList();
        var topIds = top.Select(x => x.StockId).ToList();
        var names = await _context.Stocks
            .AsNoTracking()
            .Where(s => topIds.Contains(s.Id))
            .Select(s => new { s.Id, s.StockCode, BankShortName = s.Bank != null ? s.Bank.ShortName : string.Empty })
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        model.TopVolumeStocks = top
            .Select(x => new TopTradedStockItem(
                names[x.StockId].StockCode,
                names[x.StockId].BankShortName,
                x.Volume,
                x.Value,
                model.PeriodTotalVolume > 0 ? Math.Round(x.Volume * 100m / model.PeriodTotalVolume, 1) : 0m))
            .ToList();
    }

    /// <summary>Lệnh/khớp lệnh giả lập của Investor trong hệ thống.</summary>
    private async Task FillInternalTradingAsync(AdminDashboardViewModel model, CancellationToken cancellationToken)
    {
        var trades = (await _context.Trades
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Quantity = g.Sum(t => (long)t.Quantity), Value = g.Sum(t => t.Quantity * t.Price) })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        model.TradeCount = trades?.Count ?? 0;
        model.TradeQuantity = trades?.Quantity ?? 0;
        model.TradeValue = trades?.Value ?? 0m;
        model.PendingOrderCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
    }

    private static AdminRecentPriceUpdateItem ToRecentUpdate(string stockCode, string bankShortName, PriceHistory price)
    {
        var change = price.Close - price.RefPrice;
        var statusClass =
            price.CeilingPrice > 0 && price.Close >= price.CeilingPrice ? "stock-ceil"
            : price.FloorPrice > 0 && price.Close <= price.FloorPrice ? "stock-floor"
            : price.Close > price.RefPrice ? "stock-up"
            : price.Close < price.RefPrice ? "stock-down"
            : "stock-ref";

        return new AdminRecentPriceUpdateItem
        {
            StockCode = stockCode,
            BankShortName = bankShortName,
            ClosePrice = price.Close,
            Change = change,
            ChangePercent = price.RefPrice > 0 ? change / price.RefPrice * 100 : 0,
            Volume = price.Volume,
            TradeDate = price.TradeDate,
            StatusClass = statusClass
        };
    }
}

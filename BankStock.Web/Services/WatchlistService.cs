using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class WatchlistService : IWatchlistService
{
    private readonly AppDbContext _context;
    private readonly IStockService _stockService;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(AppDbContext context, IStockService stockService, ILogger<WatchlistService> logger)
    {
        _context = context;
        _stockService = stockService;
        _logger = logger;
    }

    public async Task<List<WatchlistItemDto>> GetItemsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.Watchlists
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.AddedAt)
            .Select(w => new { w.Stock!.StockCode, w.AddedAt })
            .ToListAsync(cancellationToken);

        // Lấy giá qua StockService (một truy vấn, giữ thứ tự); mã ngừng giao dịch sẽ không có giá và bị bỏ qua.
        var quotes = await _stockService.GetBoardItemsByCodesAsync(rows.Select(r => r.StockCode).ToList(), cancellationToken);
        var addedAt = rows.ToDictionary(r => r.StockCode, r => r.AddedAt);

        return quotes
            .Select(q => new WatchlistItemDto(StockQuoteDto.From(q), addedAt[q.StockCode]))
            .ToList();
    }

    public Task<bool> ContainsAsync(string userId, string stockCode, CancellationToken cancellationToken = default)
    {
        var code = Normalize(stockCode);
        return _context.Watchlists.AnyAsync(w => w.UserId == userId && w.Stock!.StockCode == code, cancellationToken);
    }

    public async Task<(ServiceResult Result, bool InWatchlist)> ToggleAsync(string userId, string stockCode, CancellationToken cancellationToken = default)
    {
        var code = Normalize(stockCode);
        var stockId = await _context.Stocks
            .Where(s => s.StockCode == code && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (stockId is null)
        {
            return (ServiceResult.Invalid("OrderStockNotTradable", "StockCode", code), false);
        }

        var existing = await _context.Watchlists.FirstOrDefaultAsync(w => w.UserId == userId && w.StockId == stockId, cancellationToken);
        if (existing is not null)
        {
            _context.Watchlists.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return (ServiceResult.Success(stockId), false);
        }

        if (await _context.Watchlists.CountAsync(w => w.UserId == userId, cancellationToken) >= TradingRules.MaxWatchlistItems)
        {
            return (ServiceResult.Conflict("WatchlistFull", null, TradingRules.MaxWatchlistItems), false);
        }

        _context.Watchlists.Add(new Watchlist { UserId = userId, StockId = stockId.Value, AddedAt = DateTime.UtcNow });
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Hai request ghim cùng lúc vi phạm unique (UserId, StockId): mã đã nằm trong watchlist.
            _logger.LogInformation(ex, "Watchlist {StockCode} already added by concurrent request for {UserId}", code, userId);
            _context.ChangeTracker.Clear();
        }
        return (ServiceResult.Success(stockId), true);
    }

    public async Task<ServiceResult> RemoveAsync(string userId, string stockCode, CancellationToken cancellationToken = default)
    {
        var code = Normalize(stockCode);
        var existing = await _context.Watchlists.FirstOrDefaultAsync(w => w.UserId == userId && w.Stock!.StockCode == code, cancellationToken);
        if (existing is null)
        {
            return ServiceResult.NotFound();
        }

        _context.Watchlists.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(existing.Id);
    }

    private static string Normalize(string stockCode) => stockCode.Trim().ToUpperInvariant();
}

using System.Globalization;
using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class PriceHistoryAdminService : IPriceHistoryAdminService
{
    /// <summary>Giá tham chiếu mặc định khi mã chưa có phiên nào (bằng mệnh giá).</summary>
    private const decimal DefaultRefPrice = 10_000m;

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IOrderService _orderService;
    private readonly ILogger<PriceHistoryAdminService> _logger;

    public PriceHistoryAdminService(AppDbContext context, IAuditLogService auditLog, IOrderService orderService, ILogger<PriceHistoryAdminService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<PaginatedList<PriceHistoryListItem>> GetPagedAsync(PriceHistoryListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var prices = _context.PriceHistories.AsNoTracking();

        if (query.StockId is { } stockId)
        {
            prices = prices.Where(p => p.StockId == stockId);
        }
        if (query.From is { } from)
        {
            var fromDate = from.Date;
            prices = prices.Where(p => p.TradeDate >= fromDate);
        }
        if (query.To is { } to)
        {
            var toDate = to.Date;
            prices = prices.Where(p => p.TradeDate <= toDate);
        }

        return await prices
            .OrderByDescending(p => p.TradeDate)
            .ThenBy(p => p.Stock!.StockCode)
            .Select(p => new PriceHistoryListItem(
                p.Id, p.StockId, p.Stock!.StockCode, p.TradeDate,
                p.RefPrice, p.CeilingPrice, p.FloorPrice, p.Open, p.High, p.Low, p.Close, p.Volume))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<PriceHistoryFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PriceHistoryFormViewModel
            {
                Id = p.Id,
                StockId = p.StockId,
                TradeDate = p.TradeDate,
                RefPrice = p.RefPrice,
                CeilingPrice = p.CeilingPrice,
                FloorPrice = p.FloorPrice,
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(PriceHistoryFormViewModel model, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var price = new PriceHistory { StockId = model.StockId };
        Apply(price, model);
        _context.PriceHistories.Add(price);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("CreatePriceHistory", nameof(PriceHistory), price.Id.ToString(), await DescribeAsync(price, cancellationToken), cancellationToken);
        return ServiceResult.Success(price.Id);
    }

    public async Task<ServiceResult> UpdateAsync(PriceHistoryFormViewModel model, CancellationToken cancellationToken = default)
    {
        var price = await _context.PriceHistories.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);
        if (price is null)
        {
            return ServiceResult.NotFound();
        }

        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        price.StockId = model.StockId;
        Apply(price, model);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("UpdatePriceHistory", nameof(PriceHistory), price.Id.ToString(), await DescribeAsync(price, cancellationToken), cancellationToken);
        return ServiceResult.Success(price.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var price = await _context.PriceHistories.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (price is null)
        {
            return ServiceResult.NotFound();
        }

        var description = await DescribeAsync(price, cancellationToken);
        _context.PriceHistories.Remove(price);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("DeletePriceHistory", nameof(PriceHistory), id.ToString(), description, cancellationToken);
        return ServiceResult.Success(id);
    }

    public async Task<PriceAdjustViewModel?> GetAdjustAsync(int stockId, DateTime? tradeDate, CancellationToken cancellationToken = default)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Id == stockId)
            .Select(s => new { s.Id, s.StockCode, s.Exchange })
            .FirstOrDefaultAsync(cancellationToken);
        if (stock is null)
        {
            return null;
        }

        var date = (tradeDate ?? DateTime.UtcNow).Date;
        var model = new PriceAdjustViewModel { StockId = stock.Id, StockCode = stock.StockCode, Exchange = stock.Exchange, TradeDate = date };

        var session = await _context.PriceHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.StockId == stockId && p.TradeDate == date, cancellationToken);
        if (session is not null)
        {
            model.RefPrice = session.RefPrice;
            model.CeilingPrice = session.CeilingPrice;
            model.FloorPrice = session.FloorPrice;
            model.MatchPrice = session.Close;
            model.Volume = session.Volume;
            return model;
        }

        // Phiên mới: tham chiếu = giá đóng cửa phiên liền trước.
        var previousClose = await _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.StockId == stockId && p.TradeDate < date)
            .OrderByDescending(p => p.TradeDate)
            .Select(p => (decimal?)p.Close)
            .FirstOrDefaultAsync(cancellationToken);

        var refPrice = previousClose ?? DefaultRefPrice;
        var (ceiling, floor) = PriceRules.CalculateBand(stock.Exchange, refPrice);
        model.IsNewSession = true;
        model.RefPrice = refPrice;
        model.CeilingPrice = ceiling;
        model.FloorPrice = floor;
        model.MatchPrice = refPrice;
        return model;
    }

    public async Task<ServiceResult> AdjustAsync(PriceAdjustViewModel model, CancellationToken cancellationToken = default)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Id == model.StockId)
            .Select(s => new { s.Id, s.StockCode })
            .FirstOrDefaultAsync(cancellationToken);
        if (stock is null || model.TradeDate is null)
        {
            return ServiceResult.NotFound();
        }

        var date = model.TradeDate.Value.Date;
        var session = await _context.PriceHistories
            .FirstOrDefaultAsync(p => p.StockId == stock.Id && p.TradeDate == date, cancellationToken);

        bool isNew = session is null;
        if (session is null)
        {
            session = new PriceHistory
            {
                StockId = stock.Id,
                TradeDate = date,
                Open = model.MatchPrice,
                High = model.MatchPrice,
                Low = model.MatchPrice
            };
            _context.PriceHistories.Add(session);
        }
        else
        {
            // Giữ OHLC hợp lệ: High/Low bao trùm giá khớp mới và không vượt khỏi biên độ mới.
            session.Open = Math.Clamp(session.Open, model.FloorPrice, model.CeilingPrice);
            session.High = Math.Min(Math.Max(session.High, model.MatchPrice), model.CeilingPrice);
            session.Low = Math.Max(Math.Min(session.Low, model.MatchPrice), model.FloorPrice);
        }

        session.RefPrice = model.RefPrice;
        session.CeilingPrice = model.CeilingPrice;
        session.FloorPrice = model.FloorPrice;
        session.Close = model.MatchPrice;
        session.Volume = model.Volume;
        await _context.SaveChangesAsync(cancellationToken);
        var sessionId = session.Id;

        // Giá khớp mới có thể thỏa các lệnh Limit đang chờ của mã này.
        var matchedOrders = await _orderService.MatchPendingOrdersAsync(stock.Id, model.MatchPrice, cancellationToken);

        var detail = string.Create(CultureInfo.InvariantCulture,
            $"{stock.StockCode} {date:yyyy-MM-dd} {(isNew ? "new" : "update")}: ref={model.RefPrice:0}, ceil={model.CeilingPrice:0}, floor={model.FloorPrice:0}, match={model.MatchPrice:0}, matchedOrders={matchedOrders}");
        await _auditLog.LogCurrentUserAsync("AdjustPrice", nameof(PriceHistory), sessionId.ToString(), detail, cancellationToken);
        _logger.LogInformation("Price adjusted: {Detail}", detail);
        return ServiceResult.Success(sessionId) with { AffectedCount = matchedOrders };
    }

    public async Task<PriceBandDto?> GetBandAsync(int stockId, decimal refPrice, CancellationToken cancellationToken = default)
    {
        var exchange = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.Id == stockId)
            .Select(s => s.Exchange)
            .FirstOrDefaultAsync(cancellationToken);
        if (exchange is null)
        {
            return null;
        }

        var (ceiling, floor) = PriceRules.CalculateBand(exchange, refPrice);
        return new PriceBandDto(ceiling, floor, PriceRules.GetBandPercent(exchange) * 100);
    }

    private async Task<ServiceResult> ValidateAsync(PriceHistoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _context.Stocks.AnyAsync(s => s.Id == model.StockId, cancellationToken))
        {
            return ServiceResult.Invalid("ValSelectRequired", nameof(PriceHistoryFormViewModel.StockId));
        }

        var date = model.TradeDate!.Value.Date;
        if (await _context.PriceHistories.AnyAsync(p => p.StockId == model.StockId && p.TradeDate == date && p.Id != model.Id, cancellationToken))
        {
            return ServiceResult.Conflict("PriceSessionExists", nameof(PriceHistoryFormViewModel.TradeDate), date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        return ServiceResult.Success();
    }

    private static void Apply(PriceHistory price, PriceHistoryFormViewModel model)
    {
        price.TradeDate = model.TradeDate!.Value.Date;
        price.RefPrice = model.RefPrice;
        price.CeilingPrice = model.CeilingPrice;
        price.FloorPrice = model.FloorPrice;
        price.Open = model.Open;
        price.High = model.High;
        price.Low = model.Low;
        price.Close = model.Close;
        price.Volume = model.Volume;
    }

    private async Task<string> DescribeAsync(PriceHistory price, CancellationToken cancellationToken)
    {
        var code = await _context.Stocks.Where(s => s.Id == price.StockId).Select(s => s.StockCode).FirstOrDefaultAsync(cancellationToken);
        return string.Create(CultureInfo.InvariantCulture, $"{code} {price.TradeDate:yyyy-MM-dd} close={price.Close:0}");
    }
}

using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class StockAdminService : IStockAdminService
{
    private const decimal Billion = 1_000_000_000m;

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<StockAdminService> _logger;

    public StockAdminService(AppDbContext context, IAuditLogService auditLog, ILogger<StockAdminService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<PaginatedList<StockAdminListItem>> GetPagedAsync(StockAdminListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var stocks = _context.Stocks.AsNoTracking();

        if (query.Q is { } keyword)
        {
            stocks = stocks.Where(s => s.StockCode.Contains(keyword)
                || (s.Bank != null && (s.Bank.ShortName.Contains(keyword) || s.Bank.Name.Contains(keyword))));
        }
        if (query.Group is { } group)
        {
            stocks = stocks.Where(s => s.BankGroup == group);
        }
        if (query.Exchange is { } exchange)
        {
            stocks = stocks.Where(s => s.Exchange == exchange);
        }
        if (query.Active is { } active)
        {
            stocks = stocks.Where(s => s.IsActive == active);
        }

        return await stocks
            .OrderBy(s => s.StockCode)
            .Select(s => new StockAdminListItem(
                s.Id,
                s.StockCode,
                s.Bank != null ? s.Bank.ShortName : string.Empty,
                s.Exchange,
                s.BankGroup,
                s.IsActive,
                s.PriceHistories.OrderByDescending(p => p.TradeDate).Select(p => (decimal?)p.Close).FirstOrDefault(),
                s.PriceHistories.Max(p => (DateTime?)p.TradeDate),
                s.PE,
                s.CASA,
                s.PriceHistories.Count))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<StockFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Stocks
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StockFormViewModel
            {
                Id = s.Id,
                StockCode = s.StockCode,
                BankId = s.BankId,
                Exchange = s.Exchange,
                BankGroup = s.BankGroup,
                ListedShares = s.ListedShares,
                FaceValue = s.FaceValue,
                IsActive = s.IsActive,
                PE = s.PE,
                PB = s.PB,
                EPS = s.EPS,
                CASA = s.CASA,
                NPL = s.NPL,
                DividendYield = s.DividendYield,
                CharterCapitalBillion = s.CharterCapital / Billion
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(StockFormViewModel model, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var stock = new Stock();
        Apply(stock, model);
        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("CreateStock", nameof(Stock), stock.Id.ToString(), stock.StockCode, cancellationToken);
        _logger.LogInformation("Stock {StockCode} created (Id {StockId})", stock.StockCode, stock.Id);
        return ServiceResult.Success(stock.Id);
    }

    public async Task<ServiceResult> UpdateAsync(StockFormViewModel model, CancellationToken cancellationToken = default)
    {
        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Id == model.Id, cancellationToken);
        if (stock is null)
        {
            return ServiceResult.NotFound();
        }

        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        Apply(stock, model);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("UpdateStock", nameof(Stock), stock.Id.ToString(),
            $"{stock.StockCode} (active={stock.IsActive})", cancellationToken);
        return ServiceResult.Success(stock.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (stock is null)
        {
            return ServiceResult.NotFound();
        }

        bool inUse = await _context.Orders.AnyAsync(o => o.StockId == id, cancellationToken)
            || await _context.Trades.AnyAsync(t => t.StockId == id, cancellationToken)
            || await _context.Holdings.AnyAsync(h => h.StockId == id, cancellationToken)
            || await _context.Watchlists.AnyAsync(w => w.StockId == id, cancellationToken);
        if (inUse)
        {
            return ServiceResult.Conflict("StockDeleteInUse", null, stock.StockCode);
        }

        // PriceHistory và FinancialReport được xóa theo (cascade ở DB).
        _context.Stocks.Remove(stock);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("DeleteStock", nameof(Stock), id.ToString(), stock.StockCode, cancellationToken);
        _logger.LogInformation("Stock {StockCode} deleted (Id {StockId})", stock.StockCode, id);
        return ServiceResult.Success(id);
    }

    public Task<List<StockOptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        _context.Stocks
            .AsNoTracking()
            .OrderBy(s => s.StockCode)
            .Select(s => new StockOptionItem(s.Id, s.StockCode, s.Exchange, s.IsActive))
            .ToListAsync(cancellationToken);

    private async Task<ServiceResult> ValidateAsync(StockFormViewModel model, CancellationToken cancellationToken)
    {
        var code = model.StockCode.Trim().ToUpperInvariant();
        if (await _context.Stocks.AnyAsync(s => s.StockCode == code && s.Id != model.Id, cancellationToken))
        {
            return ServiceResult.Conflict("StockCodeTaken", nameof(StockFormViewModel.StockCode), code);
        }
        if (!await _context.Banks.AnyAsync(b => b.Id == model.BankId, cancellationToken))
        {
            return ServiceResult.Invalid("ValSelectRequired", nameof(StockFormViewModel.BankId));
        }
        if (!PriceRules.Exchanges.Contains(model.Exchange.ToUpperInvariant()))
        {
            return ServiceResult.Invalid("ValExchange", nameof(StockFormViewModel.Exchange));
        }
        return ServiceResult.Success();
    }

    private static void Apply(Stock stock, StockFormViewModel model)
    {
        stock.StockCode = model.StockCode.Trim().ToUpperInvariant();
        stock.BankId = model.BankId;
        stock.Exchange = model.Exchange.ToUpperInvariant();
        stock.BankGroup = model.BankGroup;
        stock.ListedShares = model.ListedShares;
        stock.FaceValue = model.FaceValue;
        stock.IsActive = model.IsActive;
        stock.PE = model.PE;
        stock.PB = model.PB;
        stock.EPS = model.EPS;
        stock.CASA = model.CASA;
        stock.NPL = model.NPL;
        stock.DividendYield = model.DividendYield;
        stock.CharterCapital = model.CharterCapitalBillion is { } billion
            ? billion * Billion
            : model.ListedShares * model.FaceValue;
    }
}

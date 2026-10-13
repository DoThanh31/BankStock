using System.Linq.Expressions;
using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class BankAdminService : IBankAdminService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<BankAdminService> _logger;

    public BankAdminService(AppDbContext context, IAuditLogService auditLog, ILogger<BankAdminService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<PaginatedList<BankAdminListItem>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var banks = _context.Banks.AsNoTracking();

        if (query.Q is { } keyword)
        {
            banks = banks.Where(b => b.Code.Contains(keyword) || b.Name.Contains(keyword) || b.ShortName.Contains(keyword));
        }

        return await banks
            .OrderBy(b => b.Code)
            .Select(ToListItem)
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<BankAdminListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Banks
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToListItem)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<BankFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Banks
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BankFormViewModel
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                ShortName = b.ShortName,
                Founded = b.Founded,
                Headquarters = b.Headquarters,
                Website = b.Website,
                LogoUrl = b.LogoUrl,
                Description = b.Description
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(BankFormViewModel model, CancellationToken cancellationToken = default)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        if (await _context.Banks.AnyAsync(b => b.Code == code, cancellationToken))
        {
            return ServiceResult.Conflict("BankCodeTaken", nameof(BankFormViewModel.Code), code);
        }

        var bank = new Bank();
        Apply(bank, model, code);
        _context.Banks.Add(bank);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("CreateBank", nameof(Bank), bank.Id.ToString(), bank.Code, cancellationToken);
        _logger.LogInformation("Bank {BankCode} created (Id {BankId})", bank.Code, bank.Id);
        return ServiceResult.Success(bank.Id);
    }

    public async Task<ServiceResult> UpdateAsync(BankFormViewModel model, CancellationToken cancellationToken = default)
    {
        var bank = await _context.Banks.FirstOrDefaultAsync(b => b.Id == model.Id, cancellationToken);
        if (bank is null)
        {
            return ServiceResult.NotFound();
        }

        var code = model.Code.Trim().ToUpperInvariant();
        if (await _context.Banks.AnyAsync(b => b.Code == code && b.Id != model.Id, cancellationToken))
        {
            return ServiceResult.Conflict("BankCodeTaken", nameof(BankFormViewModel.Code), code);
        }

        Apply(bank, model, code);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("UpdateBank", nameof(Bank), bank.Id.ToString(), bank.Code, cancellationToken);
        return ServiceResult.Success(bank.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var bank = await _context.Banks.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (bank is null)
        {
            return ServiceResult.NotFound();
        }

        var stockCount = await _context.Stocks.CountAsync(s => s.BankId == id, cancellationToken);
        if (stockCount > 0)
        {
            return ServiceResult.Conflict("BankDeleteHasStocks", null, bank.Code, stockCount);
        }

        _context.Banks.Remove(bank);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("DeleteBank", nameof(Bank), id.ToString(), bank.Code, cancellationToken);
        _logger.LogInformation("Bank {BankCode} deleted (Id {BankId})", bank.Code, id);
        return ServiceResult.Success(id);
    }

    public Task<List<BankOptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        _context.Banks
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .Select(b => new BankOptionItem(b.Id, b.Code, b.ShortName))
            .ToListAsync(cancellationToken);

    private static readonly Expression<Func<Bank, BankAdminListItem>> ToListItem = b => new BankAdminListItem(
        b.Id, b.Code, b.Name, b.ShortName, b.Founded, b.Headquarters, b.Website, b.LogoUrl, b.Description, b.Stocks.Count);

    private static void Apply(Bank bank, BankFormViewModel model, string code)
    {
        bank.Code = code;
        bank.Name = model.Name.Trim();
        bank.ShortName = model.ShortName.Trim();
        bank.Founded = model.Founded;
        bank.Headquarters = Clean(model.Headquarters);
        bank.Website = Clean(model.Website);
        bank.LogoUrl = Clean(model.LogoUrl);
        bank.Description = Clean(model.Description);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

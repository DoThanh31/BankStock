using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class FinancialReportAdminService : IFinancialReportAdminService
{
    private const decimal Billion = FinancialReportFormViewModel.Billion;

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public FinancialReportAdminService(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PaginatedList<FinancialReportListItem>> GetPagedAsync(FinancialReportListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var reports = _context.FinancialReports.AsNoTracking();

        if (query.StockId is { } stockId)
        {
            reports = reports.Where(r => r.StockId == stockId);
        }
        if (query.Year is { } year)
        {
            reports = reports.Where(r => r.Year == year);
        }

        return await reports
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Quarter)
            .ThenBy(r => r.Stock!.StockCode)
            .Select(r => new FinancialReportListItem(
                r.Id, r.Stock!.StockCode, r.Year, r.Quarter, r.Revenue, r.NetProfit, r.TotalAssets, r.TotalLiabilities))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<FinancialReportFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default) =>
        _context.FinancialReports
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new FinancialReportFormViewModel
            {
                Id = r.Id,
                StockId = r.StockId,
                Year = r.Year,
                Quarter = r.Quarter,
                RevenueBillion = r.Revenue / Billion,
                NetProfitBillion = r.NetProfit / Billion,
                TotalAssetsBillion = r.TotalAssets / Billion,
                TotalLiabilitiesBillion = r.TotalLiabilities / Billion
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(FinancialReportFormViewModel model, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var report = new FinancialReport();
        Apply(report, model);
        _context.FinancialReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("CreateFinancialReport", nameof(FinancialReport), report.Id.ToString(), await DescribeAsync(report, cancellationToken), cancellationToken);
        return ServiceResult.Success(report.Id);
    }

    public async Task<ServiceResult> UpdateAsync(FinancialReportFormViewModel model, CancellationToken cancellationToken = default)
    {
        var report = await _context.FinancialReports.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken);
        if (report is null)
        {
            return ServiceResult.NotFound();
        }

        var validation = await ValidateAsync(model, cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        Apply(report, model);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("UpdateFinancialReport", nameof(FinancialReport), report.Id.ToString(), await DescribeAsync(report, cancellationToken), cancellationToken);
        return ServiceResult.Success(report.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var report = await _context.FinancialReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
        {
            return ServiceResult.NotFound();
        }

        var description = await DescribeAsync(report, cancellationToken);
        _context.FinancialReports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLog.LogCurrentUserAsync("DeleteFinancialReport", nameof(FinancialReport), id.ToString(), description, cancellationToken);
        return ServiceResult.Success(id);
    }

    private async Task<ServiceResult> ValidateAsync(FinancialReportFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _context.Stocks.AnyAsync(s => s.Id == model.StockId, cancellationToken))
        {
            return ServiceResult.Invalid("ValSelectRequired", nameof(FinancialReportFormViewModel.StockId));
        }

        bool periodTaken = await _context.FinancialReports.AnyAsync(
            r => r.StockId == model.StockId && r.Year == model.Year && r.Quarter == model.Quarter && r.Id != model.Id,
            cancellationToken);
        if (periodTaken)
        {
            return ServiceResult.Conflict("ReportPeriodExists", nameof(FinancialReportFormViewModel.Quarter), model.Quarter, model.Year);
        }
        return ServiceResult.Success();
    }

    private static void Apply(FinancialReport report, FinancialReportFormViewModel model)
    {
        report.StockId = model.StockId;
        report.Year = model.Year;
        report.Quarter = model.Quarter;
        report.Revenue = Math.Round(model.RevenueBillion * Billion, 0);
        report.NetProfit = Math.Round(model.NetProfitBillion * Billion, 0);
        report.TotalAssets = Math.Round(model.TotalAssetsBillion * Billion, 0);
        report.TotalLiabilities = Math.Round(model.TotalLiabilitiesBillion * Billion, 0);
    }

    private async Task<string> DescribeAsync(FinancialReport report, CancellationToken cancellationToken)
    {
        var code = await _context.Stocks.Where(s => s.Id == report.StockId).Select(s => s.StockCode).FirstOrDefaultAsync(cancellationToken);
        return $"{code} Q{report.Quarter}/{report.Year}";
    }
}

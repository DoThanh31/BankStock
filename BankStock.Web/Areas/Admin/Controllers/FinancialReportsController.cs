using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class FinancialReportsController : AdminControllerBase
{
    private const string FormPartial = "_Form";

    private readonly IFinancialReportAdminService _reportService;
    private readonly IStockAdminService _stockService;

    public FinancialReportsController(IFinancialReportAdminService reportService, IStockAdminService stockService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _reportService = reportService;
        _stockService = stockService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] FinancialReportListQuery query, CancellationToken cancellationToken)
    {
        var items = await _reportService.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<FinancialReportListItem, FinancialReportListQuery>(query, items)
        {
            StockOptions = await _stockService.GetOptionsAsync(cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? stockId, CancellationToken cancellationToken)
    {
        await LoadStockOptionsAsync(cancellationToken);
        var today = DateTime.UtcNow;
        return PartialView(FormPartial, new FinancialReportFormViewModel
        {
            StockId = stockId ?? 0,
            Year = today.Year,
            Quarter = (today.Month - 1) / 3 + 1
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(FinancialReportFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = 0;
        return await SaveAsync(model, () => _reportService.CreateAsync(model, cancellationToken), cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _reportService.GetForEditAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        await LoadStockOptionsAsync(cancellationToken);
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, FinancialReportFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        return await SaveAsync(model, () => _reportService.UpdateAsync(model, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _reportService.DeleteAsync(id, cancellationToken);
        return ActionResultJson(result, "AdminDeleted");
    }

    private async Task<IActionResult> SaveAsync(FinancialReportFormViewModel model, Func<Task<ServiceResult>> save, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadStockOptionsAsync(cancellationToken);
            return InvalidForm(FormPartial, model);
        }

        var result = await save();
        if (!result.Succeeded)
        {
            await LoadStockOptionsAsync(cancellationToken);
        }
        return FormResult(result, FormPartial, model, "AdminSavedReport");
    }

    private async Task LoadStockOptionsAsync(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetOptionsAsync(cancellationToken);
        ViewData["StockOptions"] = stocks
            .Select(s => new SelectListItem(s.StockCode, s.Id.ToString()))
            .ToList();
    }
}

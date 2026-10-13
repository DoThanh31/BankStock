using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class StocksController : AdminControllerBase
{
    private const string FormPartial = "_Form";

    private readonly IStockAdminService _stockService;
    private readonly IBankAdminService _bankService;

    public StocksController(IStockAdminService stockService, IBankAdminService bankService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _stockService = stockService;
        _bankService = bankService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] StockAdminListQuery query, CancellationToken cancellationToken)
    {
        var items = await _stockService.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<StockAdminListItem, StockAdminListQuery>(query, items));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadBankOptionsAsync(cancellationToken);
        return PartialView(FormPartial, new StockFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(StockFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = 0;
        if (!ModelState.IsValid)
        {
            await LoadBankOptionsAsync(cancellationToken);
            return InvalidForm(FormPartial, model);
        }

        var result = await _stockService.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            await LoadBankOptionsAsync(cancellationToken);
        }
        return FormResult(result, FormPartial, model, "AdminSavedStock", model.StockCode);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _stockService.GetForEditAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        await LoadBankOptionsAsync(cancellationToken);
        return PartialView(FormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, StockFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await LoadBankOptionsAsync(cancellationToken);
            return InvalidForm(FormPartial, model);
        }

        var result = await _stockService.UpdateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            await LoadBankOptionsAsync(cancellationToken);
        }
        return FormResult(result, FormPartial, model, "AdminSavedStock", model.StockCode);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _stockService.DeleteAsync(id, cancellationToken);
        return ActionResultJson(result, "AdminDeleted");
    }

    private async Task LoadBankOptionsAsync(CancellationToken cancellationToken)
    {
        var banks = await _bankService.GetOptionsAsync(cancellationToken);
        ViewData["BankOptions"] = banks
            .Select(b => new SelectListItem($"{b.Code} — {b.ShortName}", b.Id.ToString()))
            .ToList();
    }
}

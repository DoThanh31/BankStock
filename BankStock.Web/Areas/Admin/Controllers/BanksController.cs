using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class BanksController : AdminControllerBase
{
    private const string FormPartial = "_Form";

    private readonly IBankAdminService _bankService;

    public BanksController(IBankAdminService bankService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _bankService = bankService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] PagedListQuery query, CancellationToken cancellationToken)
    {
        var items = await _bankService.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<BankAdminListItem, PagedListQuery>(query, items));
    }

    [HttpGet]
    public IActionResult Create() => PartialView(FormPartial, new BankFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(BankFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = 0;
        if (!ModelState.IsValid)
        {
            return InvalidForm(FormPartial, model);
        }

        var result = await _bankService.CreateAsync(model, cancellationToken);
        return FormResult(result, FormPartial, model, "AdminSavedBank", model.Code);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _bankService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : PartialView(FormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BankFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return InvalidForm(FormPartial, model);
        }

        var result = await _bankService.UpdateAsync(model, cancellationToken);
        return FormResult(result, FormPartial, model, "AdminSavedBank", model.Code);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _bankService.DeleteAsync(id, cancellationToken);
        return ActionResultJson(result, "AdminDeleted");
    }
}

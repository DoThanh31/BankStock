using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class WalletsController : AdminControllerBase
{
    private readonly IWalletAdminService _walletService;

    public WalletsController(IWalletAdminService walletService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _walletService = walletService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] PagedListQuery query, CancellationToken cancellationToken)
    {
        var items = await _walletService.GetPagedAsync(query, cancellationToken);
        var model = new PagedListViewModel<WalletListItem, PagedListQuery>(query, items);
        if (IsAjaxRequest)
        {
            return PartialView("_List", model);
        }

        ViewData["WalletStats"] = await _walletService.GetStatsAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Transactions(int id, CancellationToken cancellationToken)
    {
        var model = await _walletService.GetRecentTransactionsAsync(id, cancellationToken);
        return model is null ? NotFound() : PartialView("_Transactions", model);
    }
}

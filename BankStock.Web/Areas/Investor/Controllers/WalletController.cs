using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

public class WalletController : InvestorControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _walletService = walletService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] WalletTransactionQuery query, CancellationToken cancellationToken)
    {
        var transactions = new PagedListViewModel<WalletTransactionItem, WalletTransactionQuery>(
            query, await _walletService.GetTransactionsAsync(CurrentUserId, query, cancellationToken));
        if (IsAjaxRequest)
        {
            return PartialView("_Transactions", transactions);
        }

        var summary = await _walletService.GetSummaryAsync(CurrentUserId, cancellationToken);
        if (summary is null)
        {
            return NotFound();
        }

        return View(new WalletPageViewModel { Summary = summary, Transactions = transactions });
    }

    /// <summary>Nạp tiền giả lập (AJAX). Trả về số dư mới để cập nhật ngay trên giao diện.</summary>
    [HttpPost]
    public Task<IActionResult> Deposit(CashTransferViewModel model, CancellationToken cancellationToken) =>
        TransferAsync(model, _walletService.DepositAsync, "WalletDeposited", cancellationToken);

    [HttpPost]
    public Task<IActionResult> Withdraw(CashTransferViewModel model, CancellationToken cancellationToken) =>
        TransferAsync(model, _walletService.WithdrawAsync, "WalletWithdrawn", cancellationToken);

    private async Task<IActionResult> TransferAsync(
        CashTransferViewModel model,
        Func<string, decimal, CancellationToken, Task<ServiceResult>> transfer,
        string successKey,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return JsonError(StatusCodes.Status400BadRequest, FirstModelError());
        }

        var result = await transfer(CurrentUserId, model.Amount, cancellationToken);
        if (!result.Succeeded)
        {
            return JsonError(StatusCodeOf(result), ErrorMessage(result));
        }

        var wallet = await _walletService.GetSummaryAsync(CurrentUserId, cancellationToken);
        return Json(new { success = true, message = Localizer[successKey, model.Amount.ToString("N0")].Value, wallet });
    }
}

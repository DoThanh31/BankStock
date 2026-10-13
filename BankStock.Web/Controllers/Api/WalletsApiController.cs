using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Ví ảo của Investor đang đăng nhập: số dư, lịch sử giao dịch tiền, nạp / rút giả lập.</summary>
[Authorize(Roles = AppRoles.Investor)]
[Route(ApiDefaults.RoutePrefix + "/wallets")]
public class WalletsApiController : ApiControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsApiController(IWalletService walletService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _walletService = walletService;
    }

    /// <summary>Số dư khả dụng, phong tỏa và tổng tiền.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(WalletSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletSummaryDto>> GetMine(CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetSummaryAsync(CurrentUserId, cancellationToken);
        return wallet is null ? NotFoundProblem(Localizer["WalletNotFound"].Value) : Ok(wallet);
    }

    /// <summary>Lịch sử giao dịch tiền (mới nhất trước), lọc theo loại, phân trang.</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PagedResultDto<WalletTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResultDto<WalletTransactionDto>>> GetTransactions([FromQuery] WalletTransactionApiQuery query, CancellationToken cancellationToken)
    {
        var page = await _walletService.GetTransactionsAsync(CurrentUserId, query.ToListQuery(), cancellationToken);
        return Ok(PagedResultDto<WalletTransactionDto>.From(page, WalletTransactionDto.From));
    }

    /// <summary>
    /// Nạp / rút tiền giả lập (100.000 – 10 tỷ VNĐ, số nguyên đồng). Rút không vượt số dư khả dụng.
    /// Trả về số dư ví sau giao dịch.
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(WalletSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalletSummaryDto>> CreateTransaction([FromBody] CashTransferRequest request, CancellationToken cancellationToken)
    {
        var result = request.Type == CashTransferType.Deposit
            ? await _walletService.DepositAsync(CurrentUserId, request.Amount, cancellationToken)
            : await _walletService.WithdrawAsync(CurrentUserId, request.Amount, cancellationToken);
        if (!result.Succeeded)
        {
            return ProblemFrom(result);
        }

        return Ok(await _walletService.GetSummaryAsync(CurrentUserId, cancellationToken));
    }
}

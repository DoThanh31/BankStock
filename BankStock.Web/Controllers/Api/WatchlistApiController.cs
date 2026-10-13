using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Watchlist của Investor đang đăng nhập.</summary>
[Authorize(Roles = AppRoles.Investor)]
[Route(ApiDefaults.RoutePrefix + "/watchlist")]
public class WatchlistApiController : ApiControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistApiController(IWatchlistService watchlistService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _watchlistService = watchlistService;
    }

    /// <summary>Các mã đang theo dõi kèm giá mới nhất.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<WatchlistItemDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await _watchlistService.GetItemsAsync(CurrentUserId, cancellationToken));

    /// <summary>Ghim nếu chưa có, bỏ ghim nếu đã có. Trả về trạng thái mới.</summary>
    [HttpPost("toggle")]
    [ProducesResponseType(typeof(WatchlistToggleResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WatchlistToggleResultDto>> Toggle([FromBody] WatchlistToggleRequest request, CancellationToken cancellationToken)
    {
        var (result, inWatchlist) = await _watchlistService.ToggleAsync(CurrentUserId, request.StockCode, cancellationToken);
        if (!result.Succeeded)
        {
            return ProblemFrom(result);
        }

        return Ok(new WatchlistToggleResultDto(request.StockCode.ToUpperInvariant(), inWatchlist));
    }

    /// <summary>Bỏ một mã khỏi watchlist.</summary>
    [HttpDelete("{code:alpha:length(3)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
    {
        var result = await _watchlistService.RemoveAsync(CurrentUserId, code, cancellationToken);
        return result.Succeeded ? NoContent() : ProblemFrom(result);
    }
}

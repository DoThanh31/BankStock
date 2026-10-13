using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>Tóm tắt tài sản của Investor đang đăng nhập (cookie auth). Chỉ đọc dữ liệu của chính người gọi.</summary>
[Authorize(Roles = AppRoles.Investor)]
[Route(ApiDefaults.RoutePrefix + "/portfolio")]
public class PortfolioApiController : ApiControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioApiController(IPortfolioService portfolioService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _portfolioService = portfolioService;
    }

    /// <summary>Tiền, giá trị cổ phiếu theo giá thị trường, lãi/lỗ chưa thực hiện và lãi/lỗ tổng.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(PortfolioSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _portfolioService.GetSummaryAsync(CurrentUserId, cancellationToken);
        return summary is null ? NotFoundProblem(Localizer["WalletNotFound"].Value) : Ok(summary);
    }
}

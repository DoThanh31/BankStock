using BankStock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

public class PortfolioController : InvestorControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _portfolioService = portfolioService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _portfolioService.GetSummaryAsync(CurrentUserId, cancellationToken);
        return summary is null ? NotFound() : View(summary);
    }
}

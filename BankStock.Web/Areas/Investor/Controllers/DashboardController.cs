using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

/// <summary>Dashboard viết bằng AngularJS; dữ liệu lấy qua REST API (/api/v1/portfolio/summary, /api/v1/watchlist).</summary>
public class DashboardController : InvestorControllerBase
{
    public DashboardController(IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
    }

    [HttpGet]
    public IActionResult Index() => View();
}

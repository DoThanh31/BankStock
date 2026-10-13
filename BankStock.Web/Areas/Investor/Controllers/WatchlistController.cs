using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

/// <summary>Trang Watchlist (AngularJS): thêm / xóa mã qua /api/v1/watchlist.</summary>
public class WatchlistController : InvestorControllerBase
{
    public WatchlistController(IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
    }

    [HttpGet]
    public IActionResult Index() => View();
}

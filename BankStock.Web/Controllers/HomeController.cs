using System.Diagnostics;
using BankStock.Web.Localization;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BankStock.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStockService _stockService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IStockService stockService, ILogger<HomeController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var stocks = await _stockService.GetTopStockBoardAsync(cancellationToken);
        var overview = await _stockService.GetMarketOverviewAsync(cancellationToken);

        var viewModel = new HomeIndexViewModel
        {
            MarketOverview = overview,
            Stocks = stocks
        };

        return View(viewModel);
    }

    public IActionResult About()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        // Chỉ nhận culture được hỗ trợ; giá trị lạ bị bỏ qua (giữ ngôn ngữ hiện tại).
        var segment = UrlCulture.SegmentOf(culture);
        if (segment is not null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                AppCookies.Preference(Request));
        }

        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith('/')
            ? returnUrl
            : "/";

        // Đang ở URL có tiền tố culture (/en/...) thì chuyển sang tiền tố mới (/vi/...); returnUrl không chứa tiền tố.
        if (segment is not null && UrlCulture.FromRequest(HttpContext) is not null)
        {
            return LocalRedirect($"/{segment}{target}");
        }

        return LocalRedirect(target);
    }

    [Route("Home/StatusCodeError/{statusCode}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodeError(int statusCode)
    {
        _logger.LogWarning("HTTP Status Code Error occurred: {StatusCode}, Request: {Path}", statusCode, HttpContext.Request.Path);

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };

        return View("StatusCodeError", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = 500
        };

        return View(model);
    }
}

using System.Diagnostics;
using System.Security.Claims;
using BankStock.Web.Localization;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BankStock.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStockService _stockService;
    private readonly IAccountService _accountService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IStockService stockService, IAccountService accountService, ILogger<HomeController> logger)
    {
        _stockService = stockService;
        _accountService = accountService;
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

    /// <summary>Chính sách quyền riêng tư: liệt kê cookie ứng dụng sử dụng và mục đích.</summary>
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLanguage(string culture, string returnUrl, CancellationToken cancellationToken)
    {
        // Chỉ nhận culture được hỗ trợ; giá trị lạ bị bỏ qua (giữ ngôn ngữ hiện tại).
        var canonical = CultureCookie.Write(HttpContext, culture);
        var segment = UrlCulture.SegmentOf(canonical);

        // Đã đăng nhập: lưu vào hồ sơ để lần đăng nhập sau (trình duyệt khác) vẫn đúng ngôn ngữ.
        if (canonical is not null && User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId)
        {
            await _accountService.SetPreferredLanguageAsync(userId, canonical, cancellationToken);
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
    // Trang lỗi có thể được re-execute từ một POST (vd. 404, 429 khi đăng nhập quá nhiều lần): không kiểm tra antiforgery lần nữa.
    [IgnoreAntiforgeryToken]
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

    [IgnoreAntiforgeryToken]
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

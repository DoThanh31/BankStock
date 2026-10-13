using System.Security.Claims;
using BankStock.Web.Filters;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankStock.Web.Controllers;

[AllowAnonymous]
[Route("Stocks")]
public class StocksController : Controller
{
    private readonly IStockService _stockService;
    private readonly IRecentlyViewedStocksService _recentlyViewed;
    private readonly IWatchlistService _watchlistService;

    public StocksController(IStockService stockService, IRecentlyViewedStocksService recentlyViewed, IWatchlistService watchlistService)
    {
        _stockService = stockService;
        _recentlyViewed = recentlyViewed;
        _watchlistService = watchlistService;
    }

    [HttpGet("")]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] StockListQuery query, CancellationToken cancellationToken)
    {
        // Tham số không hợp lệ (vd. page=-1) được Normalize() đưa về mặc định thay vì báo lỗi.
        var stocks = await _stockService.GetStockListAsync(query, cancellationToken);

        var viewModel = new StockListViewModel
        {
            Query = query,
            Stocks = stocks,
            RecentlyViewed = await _stockService.GetBoardItemsByCodesAsync(_recentlyViewed.GetCodes(), cancellationToken)
        };

        if (IsAjaxRequest())
        {
            return PartialView("_StockListResults", viewModel);
        }

        return View(viewModel);
    }

    [HttpGet("{code:alpha:length(3)}")]
    public async Task<IActionResult> Details(string code, CancellationToken cancellationToken)
    {
        var detail = await _stockService.GetStockDetailAsync(code, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var previousCodes = _recentlyViewed.GetCodes()
            .Where(c => !string.Equals(c, detail.Quote.StockCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        detail.RecentlyViewed = await _stockService.GetBoardItemsByCodesAsync(previousCodes, cancellationToken);

        _recentlyViewed.Add(detail.Quote.StockCode);

        if (User.IsInRole(AppRoles.Investor))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            detail.InWatchlist = await _watchlistService.ContainsAsync(userId, detail.Quote.StockCode, cancellationToken);
        }

        return View(detail);
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}

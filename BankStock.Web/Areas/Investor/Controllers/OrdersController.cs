using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

/// <summary>Sổ lệnh (Order History) và lịch sử khớp lệnh (Trade History) của người dùng, phân trang phía server.</summary>
public class OrdersController : InvestorControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] OrderListQuery query, CancellationToken cancellationToken)
    {
        var items = await _orderService.GetOrdersAsync(CurrentUserId, query, cancellationToken);
        return ListView(new PagedListViewModel<OrderListItem, OrderListQuery>(query, items)
        {
            StockOptions = await _orderService.GetTradableStocksAsync(cancellationToken)
        });
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Trades([FromQuery] TradeListQuery query, CancellationToken cancellationToken)
    {
        var items = await _orderService.GetTradesAsync(CurrentUserId, query, cancellationToken);
        return ListView(new PagedListViewModel<TradeListItem, TradeListQuery>(query, items)
        {
            StockOptions = await _orderService.GetTradableStocksAsync(cancellationToken)
        }, "_TradeList");
    }

    /// <summary>Hủy lệnh chờ của chính người dùng; lệnh của người khác trả 404 (chống IDOR).</summary>
    [HttpPost]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelOrderAsync(CurrentUserId, id, cancellationToken);
        return ActionResultJson(result, "OrderCancelled", id);
    }
}

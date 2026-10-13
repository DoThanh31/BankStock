using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using BankStock.Web.ViewModels.Investor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class OrdersController : AdminControllerBase
{
    private readonly IOrderAdminService _orderService;
    private readonly IStockAdminService _stockService;

    public OrdersController(IOrderAdminService orderService, IStockAdminService stockService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _orderService = orderService;
        _stockService = stockService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] OrderListQuery query, CancellationToken cancellationToken)
    {
        var items = await _orderService.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<AdminOrderListItem, OrderListQuery>(query, items)
        {
            StockOptions = await _stockService.GetOptionsAsync(cancellationToken)
        });
    }
}

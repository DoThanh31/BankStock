using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

/// <summary>
/// Đặt lệnh mua/bán bằng AJAX: bấm Mua → POST → SweetAlert2 báo kết quả và cập nhật số dư ngay.
/// Kèm "giỏ lệnh nháp" lưu trong Session để soạn trước nhiều lệnh rồi đặt dần.
/// </summary>
public class TradeController : InvestorControllerBase
{
    private const int PendingListSize = 20;

    private readonly IOrderService _orderService;
    private readonly IOrderDraftService _draftService;

    public TradeController(IOrderService orderService, IOrderDraftService draftService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _orderService = orderService;
        _draftService = draftService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? code, OrderType? side, CancellationToken cancellationToken)
    {
        var stocks = await _orderService.GetTradableStocksAsync(cancellationToken);
        var selected = stocks.FirstOrDefault(s => string.Equals(s.StockCode, code, StringComparison.OrdinalIgnoreCase)) ?? stocks.FirstOrDefault();

        var quote = selected is null ? null : await _orderService.GetQuoteAsync(CurrentUserId, selected.StockCode, cancellationToken);
        var form = new PlaceOrderViewModel
        {
            StockCode = selected?.StockCode ?? string.Empty,
            Type = side ?? OrderType.Buy,
            Price = quote?.MatchPrice
        };

        return View(new TradePageViewModel
        {
            Form = form,
            Stocks = stocks,
            Quote = quote,
            PendingOrders = await GetPendingAsync(cancellationToken),
            Drafts = _draftService.GetAll(CurrentUserId)
        });
    }

    /// <summary>Báo giá + số dư khả dụng cho form (gọi khi đổi mã hoặc sau khi đặt lệnh).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(TradeQuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Quote(string code, CancellationToken cancellationToken)
    {
        var quote = await _orderService.GetQuoteAsync(CurrentUserId, code ?? string.Empty, cancellationToken);
        return quote is null ? JsonError(StatusCodes.Status404NotFound, Localizer["OrderStockNotTradable", code ?? string.Empty].Value) : Json(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Place(PlaceOrderViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return JsonError(StatusCodes.Status400BadRequest, FirstModelError());
        }

        var command = new PlaceOrderCommand(model.StockCode, model.Type, model.Mode, model.Quantity,
            model.Mode == OrderMode.Limit ? model.Price : null);
        var (result, order) = await _orderService.PlaceOrderAsync(CurrentUserId, command, cancellationToken);
        if (!result.Succeeded || order is null)
        {
            return JsonError(StatusCodeOf(result), ErrorMessage(result));
        }

        // Đặt từ lệnh nháp thành công thì bỏ lệnh đó khỏi giỏ.
        bool draftUsed = model.DraftId is { } draftId && _draftService.Remove(CurrentUserId, draftId);

        var side = Localizer["OrderType" + order.Type].Value;
        var message = order.Status == OrderStatus.Matched
            ? Localizer["OrderMatchedNow", side, order.Quantity.ToString("N0"), order.StockCode, order.MatchedPrice!.Value.ToString("N0"), order.Fee.ToString("N0")].Value
            : Localizer["OrderPendingNow", side, order.Quantity.ToString("N0"), order.StockCode, order.Price.ToString("N0")].Value;

        return Json(new { success = true, message, order, draftUsed });
    }

    /// <summary>Partial danh sách lệnh chờ khớp của người dùng (tải lại sau khi đặt / hủy lệnh).</summary>
    [HttpGet]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        return PartialView("_PendingOrders", await GetPendingAsync(cancellationToken));
    }

    /// <summary>Partial giỏ lệnh nháp (tải lại sau khi lưu / xóa / đặt lệnh nháp).</summary>
    [HttpGet]
    public IActionResult Drafts()
    {
        return PartialView("_OrderDrafts", _draftService.GetAll(CurrentUserId));
    }

    /// <summary>Lưu nội dung form hiện tại vào giỏ lệnh nháp (chưa giam tiền / cổ phiếu).</summary>
    [HttpPost]
    public IActionResult SaveDraft(PlaceOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return JsonError(StatusCodes.Status400BadRequest, FirstModelError());
        }

        return ActionResultJson(_draftService.Add(CurrentUserId, model), "OrderDraftSaved");
    }

    [HttpPost]
    public IActionResult RemoveDraft(Guid id)
    {
        return _draftService.Remove(CurrentUserId, id)
            ? JsonSuccess("OrderDraftRemoved")
            : JsonError(StatusCodes.Status404NotFound, Localizer["OrderDraftNotFound"].Value);
    }

    [HttpPost]
    public IActionResult ClearDrafts()
    {
        _draftService.Clear(CurrentUserId);
        return JsonSuccess("OrderDraftCleared");
    }

    private async Task<IReadOnlyList<OrderListItem>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var query = new OrderListQuery { Status = OrderStatus.Pending, PageSize = PendingListSize };
        var orders = await _orderService.GetOrdersAsync(CurrentUserId, query, cancellationToken);
        return orders.Items;
    }
}

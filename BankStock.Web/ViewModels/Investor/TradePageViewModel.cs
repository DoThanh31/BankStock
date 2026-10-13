using BankStock.Web.Dtos;

namespace BankStock.Web.ViewModels.Investor;

/// <summary>Trang đặt lệnh: form, danh sách mã, báo giá mã đang chọn, lệnh đang chờ khớp, giỏ lệnh nháp.</summary>
public class TradePageViewModel
{
    public PlaceOrderViewModel Form { get; init; } = new();
    public IReadOnlyList<StockOptionItem> Stocks { get; init; } = [];
    public TradeQuoteDto? Quote { get; init; }
    public IReadOnlyList<OrderListItem> PendingOrders { get; init; } = [];
    public IReadOnlyList<OrderDraftItem> Drafts { get; init; } = [];
}

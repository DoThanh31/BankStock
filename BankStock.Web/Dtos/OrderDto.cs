using BankStock.Web.Models;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Dtos;

/// <summary>Một lệnh trong sổ lệnh. <c>MatchedPrice</c> chỉ có khi lệnh đã khớp.</summary>
public record OrderDto(
    int Id,
    string StockCode,
    OrderType Type,
    OrderMode Mode,
    OrderStatus Status,
    int Quantity,
    decimal Price,
    decimal? MatchedPrice,
    decimal Fee,
    decimal Value,
    bool CanCancel,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static OrderDto From(OrderListItem item) => new(
        item.Id, item.StockCode, item.Type, item.Mode, item.Status, item.Quantity, item.Price,
        item.MatchedPrice, item.Fee, item.Value, item.CanCancel, item.CreatedAt, item.UpdatedAt);
}

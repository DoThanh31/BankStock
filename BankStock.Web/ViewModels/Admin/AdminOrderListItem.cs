using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public sealed record AdminOrderListItem(
    int Id,
    string UserEmail,
    string StockCode,
    OrderType Type,
    OrderMode Mode,
    OrderStatus Status,
    int Quantity,
    decimal Price,
    decimal? MatchedPrice,
    decimal Fee,
    DateTime CreatedAt);

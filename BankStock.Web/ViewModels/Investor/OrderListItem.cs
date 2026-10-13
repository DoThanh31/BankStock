using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

public sealed record OrderListItem(
    int Id,
    string StockCode,
    OrderType Type,
    OrderMode Mode,
    OrderStatus Status,
    int Quantity,
    decimal Price,
    decimal? MatchedPrice,
    decimal Fee,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public bool CanCancel => Status == OrderStatus.Pending;

    /// <summary>Giá trị lệnh theo giá khớp (nếu đã khớp) hoặc giá đặt.</summary>
    public decimal Value => Quantity * (MatchedPrice ?? Price);
}

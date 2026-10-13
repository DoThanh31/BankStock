using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

public sealed record TradeListItem(
    int Id,
    int? OrderId,
    string StockCode,
    OrderType Type,
    int Quantity,
    decimal Price,
    decimal Fee,
    DateTime CreatedAt)
{
    public decimal Value => Quantity * Price;

    /// <summary>Tiền thực trả (mua) hoặc thực nhận (bán) sau phí.</summary>
    public decimal NetAmount => Type == OrderType.Buy ? -(Value + Fee) : Value - Fee;
}

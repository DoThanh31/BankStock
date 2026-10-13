namespace BankStock.Web.Dtos;

/// <summary>Một mã trong danh mục: khối lượng, giá vốn bình quân, giá thị trường và lãi/lỗ chưa thực hiện.</summary>
public record HoldingDto(
    string StockCode,
    string BankShortName,
    int Quantity,
    int BlockedQuantity,
    decimal AvgCost,
    decimal MarketPrice,
    decimal ChangePercent)
{
    public int TotalQuantity => Quantity + BlockedQuantity;
    public decimal CostValue => TotalQuantity * AvgCost;
    public decimal MarketValue => TotalQuantity * MarketPrice;
    public decimal UnrealizedPnl => MarketValue - CostValue;
    public decimal UnrealizedPnlPercent => CostValue > 0 ? Math.Round(UnrealizedPnl / CostValue * 100, 2) : 0m;
}

namespace BankStock.Web.Dtos;

/// <summary>
/// Tóm tắt tài sản của Investor: tiền (khả dụng + phong tỏa), giá trị cổ phiếu theo giá thị trường,
/// lãi/lỗ chưa thực hiện và lãi/lỗ tổng so với vốn đã nạp.
/// </summary>
public record PortfolioSummaryDto(
    decimal CashBalance,
    decimal BlockedCash,
    decimal NetInvested,
    int PendingOrders,
    IReadOnlyList<HoldingDto> Holdings)
{
    public decimal StockValue => Holdings.Sum(h => h.MarketValue);
    public decimal CostValue => Holdings.Sum(h => h.CostValue);
    public decimal UnrealizedPnl => StockValue - CostValue;
    public decimal UnrealizedPnlPercent => CostValue > 0 ? Math.Round(UnrealizedPnl / CostValue * 100, 2) : 0m;
    public decimal TotalAssets => CashBalance + BlockedCash + StockValue;

    /// <summary>Lãi/lỗ tổng = tổng tài sản − (cấp ban đầu + nạp − rút).</summary>
    public decimal TotalReturn => TotalAssets - NetInvested;
    public decimal TotalReturnPercent => NetInvested > 0 ? Math.Round(TotalReturn / NetInvested * 100, 2) : 0m;
}

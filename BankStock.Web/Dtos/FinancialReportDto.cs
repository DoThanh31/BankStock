using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Báo cáo tài chính quý (VNĐ).</summary>
public record FinancialReportDto(int Year, int Quarter, decimal Revenue, decimal NetProfit, decimal TotalAssets, decimal TotalLiabilities, decimal Equity)
{
    public static FinancialReportDto From(FinancialReportItemViewModel item) => new(
        item.Year, item.Quarter, item.Revenue, item.NetProfit, item.TotalAssets, item.TotalLiabilities, item.Equity);
}

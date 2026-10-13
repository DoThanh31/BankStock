namespace BankStock.Web.ViewModels.Admin;

public sealed record FinancialReportListItem(
    int Id,
    string StockCode,
    int Year,
    int Quarter,
    decimal Revenue,
    decimal NetProfit,
    decimal TotalAssets,
    decimal TotalLiabilities)
{
    public decimal Equity => TotalAssets - TotalLiabilities;
}

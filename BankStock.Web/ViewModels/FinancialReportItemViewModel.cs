namespace BankStock.Web.ViewModels;

public class FinancialReportItemViewModel
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal Revenue { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }

    public decimal Equity => TotalAssets - TotalLiabilities;
    public string PeriodLabel => $"Q{Quarter}/{Year}";
}

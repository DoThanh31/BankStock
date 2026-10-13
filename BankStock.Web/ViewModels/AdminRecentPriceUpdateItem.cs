namespace BankStock.Web.ViewModels;

public class AdminRecentPriceUpdateItem
{
    public string StockCode { get; set; } = string.Empty;
    public string BankShortName { get; set; } = string.Empty;
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public DateTime TradeDate { get; set; }
    public string StatusClass { get; set; } = "stock-ref";
}

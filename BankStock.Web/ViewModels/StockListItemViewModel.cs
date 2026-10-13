namespace BankStock.Web.ViewModels;

public class StockListItemViewModel : StockBoardItemViewModel
{
    public decimal? PE { get; set; }
    public decimal? PB { get; set; }
    public decimal? CASA { get; set; }
    public decimal? DividendYield { get; set; }
    public long ListedShares { get; set; }

    /// <summary>Vốn hóa thị trường (VNĐ) = số CP niêm yết × giá khớp.</summary>
    public decimal MarketCap => ListedShares * CurrentPrice;
}

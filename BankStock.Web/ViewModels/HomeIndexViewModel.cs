namespace BankStock.Web.ViewModels;

public class HomeIndexViewModel
{
    public MarketOverviewViewModel MarketOverview { get; set; } = new();
    public List<StockBoardItemViewModel> Stocks { get; set; } = new();
}

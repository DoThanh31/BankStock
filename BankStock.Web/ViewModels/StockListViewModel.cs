namespace BankStock.Web.ViewModels;

public class StockListViewModel
{
    public StockListQuery Query { get; set; } = new();
    public PaginatedList<StockListItemViewModel> Stocks { get; set; } = new([], 0, 1, StockListQuery.DefaultPageSize);
    public List<StockBoardItemViewModel> RecentlyViewed { get; set; } = new();
}

namespace BankStock.Web.ViewModels;

/// <summary>Model trang danh sách Admin: bộ lọc hiện tại + một trang kết quả.</summary>
public class PagedListViewModel<TItem, TQuery> where TQuery : PagedListQuery
{
    public PagedListViewModel(TQuery query, PaginatedList<TItem> items)
    {
        Query = query;
        Items = items;
    }

    public TQuery Query { get; }
    public PaginatedList<TItem> Items { get; }

    /// <summary>Danh sách mã cổ phiếu cho ô lọc (chỉ dùng ở trang giá / BCTC).</summary>
    public IReadOnlyList<StockOptionItem> StockOptions { get; init; } = [];

    /// <summary>Danh sách giá trị cho ô lọc dạng chọn (vd. loại hành động audit).</summary>
    public IReadOnlyList<string> FilterOptions { get; init; } = [];
}

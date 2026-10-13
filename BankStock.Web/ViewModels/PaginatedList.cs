namespace BankStock.Web.ViewModels;

public class PaginatedList<T>
{
    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalPages { get; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public int FirstItemNumber => TotalCount == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
    public int LastItemNumber => Math.Min(PageIndex * PageSize, TotalCount);

    /// <summary>Đổi kiểu phần tử (vd. anonymous projection → ViewModel), giữ nguyên thông tin phân trang.</summary>
    public PaginatedList<TResult> Select<TResult>(Func<T, TResult> selector) =>
        new(Items.Select(selector).ToList(), TotalCount, PageIndex, PageSize);
}

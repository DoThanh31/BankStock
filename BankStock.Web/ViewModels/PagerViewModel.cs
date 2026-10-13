namespace BankStock.Web.ViewModels;

/// <summary>
/// Model cho partial _Pagination dùng chung: trang hiện tại + hàm sinh URL cho từng trang.
/// </summary>
public class PagerViewModel
{
    public PagerViewModel(int pageIndex, int totalPages, Func<int, string?> pageUrl)
    {
        PageIndex = pageIndex;
        TotalPages = totalPages;
        PageUrl = pageUrl;
    }

    public int PageIndex { get; }
    public int TotalPages { get; }
    public Func<int, string?> PageUrl { get; }

    /// <summary>Số trang tối đa hiển thị quanh trang hiện tại.</summary>
    public int Window { get; init; } = 2;
}

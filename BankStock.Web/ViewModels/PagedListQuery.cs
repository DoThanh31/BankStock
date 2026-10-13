using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BankStock.Web.ViewModels;

/// <summary>
/// Tham số tìm kiếm + phân trang chung cho các trang danh sách Admin và Investor (bind từ query string).
/// Lớp con bổ sung bộ lọc riêng qua <see cref="AddFilterValues"/>.
/// </summary>
public class PagedListQuery : IPageSizeQuery
{
    public const int DefaultPageSize = 10;

    /// <summary>Giới hạn trên của page size (REST API cho phép tới 100); giao diện chỉ đưa ra <see cref="AllowedPageSizes"/>.</summary>
    public const int MaxPageSize = 100;
    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    [StringLength(100)]
    [Display(Name = "FieldSearch")]
    public string? Q { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public virtual bool HasFilters => !string.IsNullOrEmpty(Q);

    public bool IsSelectablePageSize(int pageSize) => AllowedPageSizes.Contains(pageSize);

    public virtual void Normalize()
    {
        Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
        if (Page < 1) Page = 1;
        if (PageSize is < 1 or > MaxPageSize) PageSize = DefaultPageSize;
    }

    /// <summary>Route values giữ nguyên bộ lọc hiện tại; chỉ đưa vào URL giá trị khác mặc định.</summary>
    public Dictionary<string, string> ToRouteValues(int? page = null)
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Q)) values["q"] = Q;
        AddFilterValues(values);

        var effectivePage = page ?? Page;
        if (effectivePage > 1) values["page"] = effectivePage.ToString(CultureInfo.InvariantCulture);
        if (PageSize != DefaultPageSize) values["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    protected virtual void AddFilterValues(Dictionary<string, string> values)
    {
    }

    protected static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

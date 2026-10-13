using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels;

public enum StockSortField { Code, Price, Change, Volume, PE, CASA, Dividend, MarketCap }

public enum SortDirection { Asc, Desc }

/// <summary>
/// Tham số lọc / sắp xếp / phân trang của trang danh sách cổ phiếu (bind từ query string).
/// </summary>
public class StockListQuery : IPageSizeQuery
{
    public const int DefaultPageSize = 5;

    /// <summary>Giới hạn trên của page size (REST API cho phép tới 100); giao diện chỉ đưa ra <see cref="AllowedPageSizes"/>.</summary>
    public const int MaxPageSize = 100;
    public static readonly int[] AllowedPageSizes = [5, 10, 20];

    [StringLength(50)]
    [Display(Name = "FieldSearch")]
    public string? Q { get; set; }

    [Display(Name = "FieldBankGroup")]
    public BankGroup? Group { get; set; }

    [Display(Name = "FilterPeUnder10")]
    public bool PeUnder10 { get; set; }

    [Display(Name = "FilterCasaOver30")]
    public bool CasaOver30 { get; set; }

    [Range(0, 100)]
    [Display(Name = "FilterMinDividend")]
    public decimal? MinDividend { get; set; }

    public StockSortField Sort { get; set; } = StockSortField.Code;

    public SortDirection Dir { get; set; } = SortDirection.Asc;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasFilters => !string.IsNullOrWhiteSpace(Q) || Group.HasValue || PeUnder10 || CasaOver30 || MinDividend.HasValue;

    public bool IsSelectablePageSize(int pageSize) => AllowedPageSizes.Contains(pageSize);

    /// <summary>
    /// Đưa tham số về giá trị hợp lệ (trang ≥ 1, page size trong khoảng 1–<see cref="MaxPageSize"/>).
    /// </summary>
    public void Normalize()
    {
        Q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
        if (Page < 1) Page = 1;
        if (PageSize is < 1 or > MaxPageSize) PageSize = DefaultPageSize;
        if (MinDividend is < 0 or > 100) MinDividend = null;
    }

    /// <summary>
    /// Sinh route values giữ nguyên bộ lọc hiện tại, cho phép ghi đè trang hoặc cột sắp xếp.
    /// Chỉ đưa vào URL các giá trị khác mặc định để URL gọn.
    /// </summary>
    public Dictionary<string, string> ToRouteValues(int? page = null, StockSortField? sort = null, SortDirection? dir = null)
    {
        var values = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Q)) values["q"] = Q;
        if (Group.HasValue) values["group"] = Group.Value.ToString();
        if (PeUnder10) values["peUnder10"] = "true";
        if (CasaOver30) values["casaOver30"] = "true";
        if (MinDividend.HasValue) values["minDividend"] = MinDividend.Value.ToString(CultureInfo.InvariantCulture);

        var effectiveSort = sort ?? Sort;
        var effectiveDir = dir ?? Dir;
        if (effectiveSort != StockSortField.Code) values["sort"] = effectiveSort.ToString();
        if (effectiveDir != SortDirection.Asc) values["dir"] = effectiveDir.ToString();

        var effectivePage = page ?? Page;
        if (effectivePage > 1) values["page"] = effectivePage.ToString(CultureInfo.InvariantCulture);
        if (PageSize != DefaultPageSize) values["pageSize"] = PageSize.ToString(CultureInfo.InvariantCulture);
        return values;
    }

    /// <summary>Route values khi bấm vào tiêu đề cột: cùng cột thì đảo chiều, cột khác thì bắt đầu tăng dần.</summary>
    public Dictionary<string, string> ToSortRouteValues(StockSortField field)
    {
        var nextDir = Sort == field && Dir == SortDirection.Asc ? SortDirection.Desc : SortDirection.Asc;
        return ToRouteValues(page: 1, sort: field, dir: nextDir);
    }
}

using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels;

public class StockDetailViewModel
{
    public StockListItemViewModel Quote { get; set; } = new();
    public DateTime? LastTradeDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal FaceValue { get; set; }
    public decimal? EPS { get; set; }
    public decimal? NPL { get; set; }
    public decimal? CharterCapital { get; set; }

    public int? Founded { get; set; }
    public string? Headquarters { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }

    public List<FinancialReportItemViewModel> FinancialReports { get; set; } = new();
    public List<StockBoardItemViewModel> RecentlyViewed { get; set; } = new();

    /// <summary>Mã có trong watchlist của Investor đang đăng nhập; null nếu không phải Investor (ẩn nút ghim).</summary>
    public bool? InWatchlist { get; set; }
}

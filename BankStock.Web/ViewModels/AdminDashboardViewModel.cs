using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.ViewModels;

public class AdminDashboardViewModel
{
    /// <summary>Số phiên gần nhất dùng để xếp hạng mã giao dịch nhiều nhất.</summary>
    public const int TopVolumePeriodDays = 30;

    public int TotalBanks { get; set; }
    public int TotalStocks { get; set; }
    public int TotalInvestors { get; set; }
    public long TotalSessionVolume { get; set; }
    public DateTime? LastSessionDate { get; set; }

    // Thống kê toàn sàn trong kỳ (theo dữ liệu giá)
    public long PeriodTotalVolume { get; set; }
    public decimal PeriodTotalValue { get; set; }
    public List<TopTradedStockItem> TopVolumeStocks { get; set; } = new();

    // Giao dịch giả lập nội bộ (Order/Trade của Investor)
    public int TradeCount { get; set; }
    public long TradeQuantity { get; set; }
    public decimal TradeValue { get; set; }
    public int PendingOrderCount { get; set; }

    public WalletStatsViewModel WalletStats { get; set; } = new();

    public List<AdminRecentPriceUpdateItem> RecentUpdates { get; set; } = new();
    public List<AuditLogListItem> RecentAuditLogs { get; set; } = new();
}

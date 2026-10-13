using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>Thống kê toàn bộ ví ảo của Investor.</summary>
public class WalletStatsViewModel
{
    public int TotalWallets { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalBalance { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalBlocked { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalGranted { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalDeposited { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalWithdrawn { get; init; }

    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    public decimal TotalFees { get; init; }

    public decimal AverageBalance => TotalWallets > 0 ? (TotalBalance + TotalBlocked) / TotalWallets : 0m;
}

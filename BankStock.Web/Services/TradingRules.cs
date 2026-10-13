namespace BankStock.Web.Services;

/// <summary>Hằng số nghiệp vụ giao dịch giả lập (dùng chung cho annotation và Service).</summary>
public static class TradingRules
{
    /// <summary>Khối lượng đặt lệnh phải là bội số của 100.</summary>
    public const int LotSize = 100;

    public const int MaxOrderQuantity = 500_000;

    /// <summary>Phí mặc định (%) nếu appsettings không có "TradingFeePercentage".</summary>
    public const decimal DefaultFeePercentage = 0.15m;

    public const double MinCashTransfer = 100_000;
    public const double MaxCashTransfer = 10_000_000_000;

    public const int MaxWatchlistItems = 30;

    /// <summary>Số lần thử lại khi ví/danh mục bị cập nhật đồng thời (RowVersion).</summary>
    public const int MaxConcurrencyRetries = 3;

    /// <summary>Phí giao dịch làm tròn tới đồng.</summary>
    public static decimal CalculateFee(decimal value, decimal feePercentage) =>
        Math.Round(value * feePercentage / 100m, 0, MidpointRounding.AwayFromZero);
}

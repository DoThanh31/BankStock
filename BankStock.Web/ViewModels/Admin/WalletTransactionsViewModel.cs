namespace BankStock.Web.ViewModels.Admin;

/// <summary>Các giao dịch gần nhất của một ví (modal xem nhanh ở trang Thống kê ví).</summary>
public class WalletTransactionsViewModel
{
    public const int MaxItems = 20;

    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal BlockedBalance { get; init; }
    public IReadOnlyList<WalletTransactionItem> Items { get; init; } = [];
}

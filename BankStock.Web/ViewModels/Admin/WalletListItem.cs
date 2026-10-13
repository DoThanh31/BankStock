namespace BankStock.Web.ViewModels.Admin;

public sealed record WalletListItem(
    int WalletId,
    string Email,
    string FullName,
    decimal Balance,
    decimal BlockedBalance,
    bool IsActive,
    DateTime LastUpdatedAt,
    int TransactionCount)
{
    public decimal Total => Balance + BlockedBalance;
}

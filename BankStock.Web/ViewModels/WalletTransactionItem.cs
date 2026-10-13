using BankStock.Web.Models;

namespace BankStock.Web.ViewModels;

public sealed record WalletTransactionItem(
    DateTime CreatedAt,
    WalletTransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description);

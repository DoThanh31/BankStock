using BankStock.Web.Models;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Một giao dịch tiền. <c>Amount</c> âm khi tiền ra; <c>BalanceAfter</c> = tổng tiền (khả dụng + phong tỏa) sau giao dịch.</summary>
public record WalletTransactionDto(DateTime CreatedAt, WalletTransactionType Type, decimal Amount, decimal BalanceAfter, string? Description)
{
    public static WalletTransactionDto From(WalletTransactionItem item) =>
        new(item.CreatedAt, item.Type, item.Amount, item.BalanceAfter, item.Description);
}

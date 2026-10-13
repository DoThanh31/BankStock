using BankStock.Web.Dtos;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Services;

/// <summary>Ví ảo của Investor. Mọi thao tác đều lọc theo <c>userId</c> của người đang đăng nhập (chống IDOR).</summary>
public interface IWalletService
{
    Task<WalletSummaryDto?> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Nạp tiền giả lập vào số dư khả dụng.</summary>
    Task<ServiceResult> DepositAsync(string userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Rút tiền giả lập; không vượt số dư khả dụng (tiền phong tỏa không rút được).</summary>
    Task<ServiceResult> WithdrawAsync(string userId, decimal amount, CancellationToken cancellationToken = default);

    Task<PaginatedList<WalletTransactionItem>> GetTransactionsAsync(string userId, WalletTransactionQuery query, CancellationToken cancellationToken = default);
}

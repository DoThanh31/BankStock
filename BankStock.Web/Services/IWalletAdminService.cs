using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>Thống kê ví ảo cho Admin (chỉ đọc).</summary>
public interface IWalletAdminService
{
    Task<WalletStatsViewModel> GetStatsAsync(CancellationToken cancellationToken = default);
    Task<PaginatedList<WalletListItem>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken = default);
    Task<WalletTransactionsViewModel?> GetRecentTransactionsAsync(int walletId, CancellationToken cancellationToken = default);
}

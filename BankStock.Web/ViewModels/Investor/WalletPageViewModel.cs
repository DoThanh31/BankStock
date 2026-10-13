using BankStock.Web.Dtos;

namespace BankStock.Web.ViewModels.Investor;

/// <summary>Trang ví: số dư, form nạp/rút và lịch sử giao dịch tiền (phân trang).</summary>
public class WalletPageViewModel
{
    public WalletSummaryDto Summary { get; init; } = new(0, 0, false);
    public CashTransferViewModel Transfer { get; init; } = new();
    public required PagedListViewModel<WalletTransactionItem, WalletTransactionQuery> Transactions { get; init; }
}

using BankStock.Web.Models;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Dtos;

/// <summary>Bộ lọc của GET /api/v1/wallets/transactions theo loại giao dịch.</summary>
public record WalletTransactionApiQuery : ApiPageQuery
{
    public WalletTransactionType? Type { get; init; }

    public WalletTransactionQuery ToListQuery() => new() { Type = Type, Page = Page, PageSize = PageSize };
}

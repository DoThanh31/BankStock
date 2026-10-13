using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class WalletAdminService : IWalletAdminService
{
    private readonly AppDbContext _context;

    public WalletAdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WalletStatsViewModel> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totals = (await _context.Wallets
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Balance = g.Sum(w => w.Balance), Blocked = g.Sum(w => w.BlockedBalance) })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        // Dấu của Amount (âm/dương) tùy loại giao dịch nên cộng theo trị tuyệt đối.
        var byType = await _context.WalletTransactions
            .AsNoTracking()
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Sum = g.Sum(t => Math.Abs(t.Amount)) })
            .ToDictionaryAsync(x => x.Type, x => x.Sum, cancellationToken);

        decimal SumOf(WalletTransactionType type) => byType.GetValueOrDefault(type);

        return new WalletStatsViewModel
        {
            TotalWallets = totals?.Count ?? 0,
            TotalBalance = totals?.Balance ?? 0m,
            TotalBlocked = totals?.Blocked ?? 0m,
            TotalGranted = SumOf(WalletTransactionType.InitialGrant),
            TotalDeposited = SumOf(WalletTransactionType.Deposit),
            TotalWithdrawn = SumOf(WalletTransactionType.Withdraw),
            TotalFees = SumOf(WalletTransactionType.Fee)
        };
    }

    public async Task<PaginatedList<WalletListItem>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var wallets = _context.Wallets.AsNoTracking();

        if (query.Q is { } keyword)
        {
            wallets = wallets.Where(w => w.User != null
                && ((w.User.Email != null && w.User.Email.Contains(keyword)) || w.User.FullName.Contains(keyword)));
        }

        return await wallets
            .OrderByDescending(w => w.Balance + w.BlockedBalance)
            .ThenBy(w => w.Id)
            .Select(w => new WalletListItem(
                w.Id,
                w.User != null ? w.User.Email ?? string.Empty : string.Empty,
                w.User != null ? w.User.FullName : string.Empty,
                w.Balance,
                w.BlockedBalance,
                w.IsActive,
                w.LastUpdatedAt,
                w.Transactions.Count))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<WalletTransactionsViewModel?> GetRecentTransactionsAsync(int walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .Where(w => w.Id == walletId)
            .Select(w => new
            {
                Email = w.User != null ? w.User.Email ?? string.Empty : string.Empty,
                FullName = w.User != null ? w.User.FullName : string.Empty,
                w.Balance,
                w.BlockedBalance
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (wallet is null)
        {
            return null;
        }

        var items = await _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Take(WalletTransactionsViewModel.MaxItems)
            .Select(t => new WalletTransactionItem(t.CreatedAt, t.Type, t.Amount, t.BalanceAfter, t.Description))
            .ToListAsync(cancellationToken);

        return new WalletTransactionsViewModel
        {
            Email = wallet.Email,
            FullName = wallet.FullName,
            Balance = wallet.Balance,
            BlockedBalance = wallet.BlockedBalance,
            Items = items
        };
    }
}

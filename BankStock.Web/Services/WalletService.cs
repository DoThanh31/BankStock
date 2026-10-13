using System.Globalization;
using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<WalletService> _logger;

    public WalletService(AppDbContext context, IAuditLogService auditLog, ILogger<WalletService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    public Task<WalletSummaryDto?> GetSummaryAsync(string userId, CancellationToken cancellationToken = default) =>
        _context.Wallets
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => new WalletSummaryDto(w.Balance, w.BlockedBalance, w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ServiceResult> DepositAsync(string userId, decimal amount, CancellationToken cancellationToken = default) =>
        TransferAsync(userId, amount, WalletTransactionType.Deposit, cancellationToken);

    public Task<ServiceResult> WithdrawAsync(string userId, decimal amount, CancellationToken cancellationToken = default) =>
        TransferAsync(userId, amount, WalletTransactionType.Withdraw, cancellationToken);

    public async Task<PaginatedList<WalletTransactionItem>> GetTransactionsAsync(string userId, WalletTransactionQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var transactions = _context.WalletTransactions
            .AsNoTracking()
            .Where(t => t.Wallet!.UserId == userId);

        if (query.Type is { } type)
        {
            transactions = transactions.Where(t => t.Type == type);
        }
        if (query.Q is { } keyword)
        {
            transactions = transactions.Where(t => t.Description != null && t.Description.Contains(keyword));
        }

        return await transactions
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(t => new WalletTransactionItem(t.CreatedAt, t.Type, t.Amount, t.BalanceAfter, t.Description))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    /// <summary>
    /// Nạp/rút: cập nhật số dư + ghi WalletTransaction trong một lần SaveChanges.
    /// Ví dùng RowVersion nên khi bị cập nhật đồng thời (vd. đang khớp lệnh) sẽ đọc lại và thử lại (<see cref="ConcurrencyRetry"/>).
    /// </summary>
    private async Task<ServiceResult> TransferAsync(string userId, decimal amount, WalletTransactionType type, CancellationToken cancellationToken)
    {
        if (amount < (decimal)TradingRules.MinCashTransfer || amount > (decimal)TradingRules.MaxCashTransfer || decimal.Round(amount, 0) != amount)
        {
            return ServiceResult.Invalid("WalletAmountInvalid", "Amount", TradingRules.MinCashTransfer, TradingRules.MaxCashTransfer);
        }

        var result = await ConcurrencyRetry.ExecuteAsync(_context, _logger, $"{type} wallet {userId}", async () =>
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
            if (wallet is null)
            {
                return ServiceResult.NotFound();
            }
            if (!wallet.IsActive)
            {
                return ServiceResult.Conflict("WalletInactive");
            }
            if (type == WalletTransactionType.Withdraw && wallet.Balance < amount)
            {
                return ServiceResult.Invalid("WalletInsufficientBalance", "Amount", wallet.Balance);
            }

            var signedAmount = type == WalletTransactionType.Withdraw ? -amount : amount;
            wallet.Balance += signedAmount;
            wallet.LastUpdatedAt = DateTime.UtcNow;
            _context.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = signedAmount,
                BalanceAfter = wallet.Balance + wallet.BlockedBalance,
                Type = type,
                Description = type == WalletTransactionType.Deposit ? "Nạp tiền giả lập" : "Rút tiền giả lập",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success(wallet.Id);
        }, () => ServiceResult.Conflict("ConcurrencyConflict"));

        if (result.Succeeded)
        {
            await _auditLog.LogAsync(type.ToString(), userId, nameof(Wallet), result.EntityId?.ToString(),
                amount.ToString("0", CultureInfo.InvariantCulture), cancellationToken);
        }
        return result;
    }
}

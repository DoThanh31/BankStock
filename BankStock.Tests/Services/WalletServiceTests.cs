using BankStock.Tests.Infrastructure;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels.Investor;
using Microsoft.EntityFrameworkCore;
using static BankStock.Tests.Infrastructure.TestDatabase;

namespace BankStock.Tests.Services;

public class WalletServiceTests
{
    private const decimal MinTransfer = (decimal)TradingRules.MinCashTransfer;
    private const decimal MaxTransfer = (decimal)TradingRules.MaxCashTransfer;

    private static async Task<ServiceResult> DepositAsync(TestDatabase db, decimal amount, string userId = InvestorId)
    {
        await using var context = db.CreateContext();
        return await db.CreateWalletService(context).DepositAsync(userId, amount);
    }

    private static async Task<ServiceResult> WithdrawAsync(TestDatabase db, decimal amount, string userId = InvestorId)
    {
        await using var context = db.CreateContext();
        return await db.CreateWalletService(context).WithdrawAsync(userId, amount);
    }

    // ---------- Nạp tiền ----------

    [Fact]
    public async Task Deposit_ValidAmount_IncreasesBalanceAndWritesLedgerAndAudit()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await DepositAsync(db, 2_000_000m);

        Assert.True(result.Succeeded);
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(InitialBalance + 2_000_000m, wallet.Balance);
        Assert.Equal(wallet.Id, result.EntityId);

        await using var context = db.CreateContext();
        var transaction = await context.WalletTransactions.SingleAsync();
        Assert.Equal(WalletTransactionType.Deposit, transaction.Type);
        Assert.Equal(2_000_000m, transaction.Amount);
        Assert.Equal(InitialBalance + 2_000_000m, transaction.BalanceAfter);
        Assert.Equal(DateTimeKind.Utc, transaction.CreatedAt.Kind);

        var audit = await context.AuditLogs.SingleAsync();
        Assert.Equal("Deposit", audit.Action);
        Assert.Equal(InvestorId, audit.UserId);
        Assert.Equal("2000000", audit.Detail);
    }

    [Theory]
    [InlineData(100_000)]          // mức tối thiểu
    [InlineData(10_000_000_000)]   // mức tối đa
    public async Task Deposit_AmountOnBoundary_IsAccepted(decimal amount)
    {
        var db = await TestDatabase.CreateAsync();

        Assert.True((await DepositAsync(db, amount)).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500_000)]
    [InlineData(99_999)]
    [InlineData(10_000_000_001)]
    [InlineData(150_000.5)] // không nhận số lẻ đồng
    public async Task Deposit_InvalidAmount_ReturnsValidationErrorAndNoChange(decimal amount)
    {
        var db = await TestDatabase.CreateAsync();

        var result = await DepositAsync(db, amount);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("WalletAmountInvalid", result.ErrorKey);
        Assert.Equal("Amount", result.Field);
        Assert.Equal(InitialBalance, (await db.GetWalletAsync(InvestorId)).Balance);
        await using var context = db.CreateContext();
        Assert.Empty(context.WalletTransactions);
        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task Deposit_UserWithoutWallet_ReturnsNotFound()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await DepositAsync(db, MinTransfer, "no-such-user");

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Deposit_InactiveWallet_ReturnsConflict()
    {
        var db = await TestDatabase.CreateAsync();
        await db.UpdateWalletAsync(InvestorId, w => w.IsActive = false);

        var result = await DepositAsync(db, MinTransfer);

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("WalletInactive", result.ErrorKey);
        Assert.Equal(InitialBalance, (await db.GetWalletAsync(InvestorId)).Balance);
    }

    [Fact]
    public async Task Deposit_DoesNotTouchOtherInvestorsWallet()
    {
        var db = await TestDatabase.CreateAsync();

        await DepositAsync(db, 5_000_000m);

        Assert.Equal(InitialBalance, (await db.GetWalletAsync(OtherInvestorId)).Balance);
    }

    // ---------- Rút tiền ----------

    [Fact]
    public async Task Withdraw_ValidAmount_DecreasesBalanceWithNegativeLedgerEntry()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await WithdrawAsync(db, 1_000_000m);

        Assert.True(result.Succeeded);
        Assert.Equal(InitialBalance - 1_000_000m, (await db.GetWalletAsync(InvestorId)).Balance);

        await using var context = db.CreateContext();
        var transaction = await context.WalletTransactions.SingleAsync();
        Assert.Equal(WalletTransactionType.Withdraw, transaction.Type);
        Assert.Equal(-1_000_000m, transaction.Amount);
        Assert.Equal(InitialBalance - 1_000_000m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_EntireAvailableBalance_LeavesZero()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await WithdrawAsync(db, InitialBalance);

        Assert.True(result.Succeeded);
        Assert.Equal(0m, (await db.GetWalletAsync(InvestorId)).Balance);
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_ReturnsInsufficientBalance()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await WithdrawAsync(db, InitialBalance + MinTransfer);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("WalletInsufficientBalance", result.ErrorKey);
        Assert.Equal([InitialBalance], result.ErrorArgs);
        Assert.Equal(InitialBalance, (await db.GetWalletAsync(InvestorId)).Balance);
    }

    [Fact]
    public async Task Withdraw_CannotUseBlockedBalance()
    {
        var db = await TestDatabase.CreateAsync();
        // 300 triệu khả dụng + 200 triệu đang phong tỏa cho lệnh mua chờ khớp.
        await db.UpdateWalletAsync(InvestorId, w =>
        {
            w.Balance = 300_000_000m;
            w.BlockedBalance = 200_000_000m;
        });

        var result = await WithdrawAsync(db, 400_000_000m);

        Assert.Equal("WalletInsufficientBalance", result.ErrorKey);
        var wallet = await db.GetWalletAsync(InvestorId);
        Assert.Equal(300_000_000m, wallet.Balance);
        Assert.Equal(200_000_000m, wallet.BlockedBalance);
    }

    [Fact]
    public async Task Withdraw_BalanceAfterIncludesBlockedBalance()
    {
        var db = await TestDatabase.CreateAsync();
        await db.UpdateWalletAsync(InvestorId, w =>
        {
            w.Balance = 300_000_000m;
            w.BlockedBalance = 200_000_000m;
        });

        await WithdrawAsync(db, 100_000_000m);

        await using var context = db.CreateContext();
        // BalanceAfter = tổng tiền (khả dụng + phong tỏa) sau giao dịch.
        Assert.Equal(400_000_000m, (await context.WalletTransactions.SingleAsync()).BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_InvalidAmount_IsValidatedBeforeBalanceCheck()
    {
        var db = await TestDatabase.CreateAsync();

        var result = await WithdrawAsync(db, MaxTransfer + 1);

        Assert.Equal("WalletAmountInvalid", result.ErrorKey);
    }

    // ---------- Truy vấn ----------

    [Fact]
    public async Task GetSummary_ReturnsBalancesAndTotal()
    {
        var db = await TestDatabase.CreateAsync();
        await db.UpdateWalletAsync(InvestorId, w =>
        {
            w.Balance = 300_000_000m;
            w.BlockedBalance = 200_000_000m;
        });

        await using var context = db.CreateContext();
        var summary = await db.CreateWalletService(context).GetSummaryAsync(InvestorId);

        Assert.NotNull(summary);
        Assert.Equal(300_000_000m, summary.Balance);
        Assert.Equal(200_000_000m, summary.BlockedBalance);
        Assert.Equal(500_000_000m, summary.Total);
        Assert.True(summary.IsActive);
    }

    [Fact]
    public async Task GetSummary_UnknownUser_ReturnsNull()
    {
        var db = await TestDatabase.CreateAsync();
        await using var context = db.CreateContext();

        Assert.Null(await db.CreateWalletService(context).GetSummaryAsync("no-such-user"));
    }

    [Fact]
    public async Task GetTransactions_ReturnsOnlyOwnTransactionsNewestFirst()
    {
        var db = await TestDatabase.CreateAsync();
        await DepositAsync(db, 1_000_000m);
        await WithdrawAsync(db, 200_000m);
        await DepositAsync(db, 9_000_000m, OtherInvestorId);

        await using var context = db.CreateContext();
        var page = await db.CreateWalletService(context).GetTransactionsAsync(InvestorId, new WalletTransactionQuery());

        Assert.Equal(2, page.TotalCount);
        Assert.Equal([-200_000m, 1_000_000m], page.Items.Select(t => t.Amount));
    }

    [Fact]
    public async Task GetTransactions_FiltersByType()
    {
        var db = await TestDatabase.CreateAsync();
        await DepositAsync(db, 1_000_000m);
        await WithdrawAsync(db, 200_000m);
        await DepositAsync(db, 3_000_000m);

        await using var context = db.CreateContext();
        var page = await db.CreateWalletService(context)
            .GetTransactionsAsync(InvestorId, new WalletTransactionQuery { Type = WalletTransactionType.Withdraw });

        var only = Assert.Single(page.Items);
        Assert.Equal(WalletTransactionType.Withdraw, only.Type);
    }

    [Fact]
    public async Task GetTransactions_PagesServerSideAndClampsPageToLast()
    {
        var db = await TestDatabase.CreateAsync();
        for (var i = 1; i <= 12; i++)
        {
            await DepositAsync(db, i * MinTransfer);
        }

        await using var context = db.CreateContext();
        var service = db.CreateWalletService(context);
        var page2 = await service.GetTransactionsAsync(InvestorId, new WalletTransactionQuery { Page = 2, PageSize = 5 });
        var beyond = await service.GetTransactionsAsync(InvestorId, new WalletTransactionQuery { Page = 99, PageSize = 5 });

        Assert.Equal(12, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal(7 * MinTransfer, page2.Items[0].Amount);
        Assert.Equal(3, beyond.PageIndex);
        Assert.Equal(2, beyond.Items.Count);
    }
}

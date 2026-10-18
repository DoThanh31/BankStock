using BankStock.Web.Data;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class AccountService : IAccountService
{
    public const decimal DefaultInitialGrant = 500_000_000m;

    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AppDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger<AccountService> logger)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterInvestorAsync(RegisterInvestorCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = command.FullName.Trim(),
            PreferredLanguage = command.PreferredLanguage,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // UserManager dùng chung AppDbContext (scoped) nên transaction bao trọn cả user, role và ví.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        result = await _userManager.AddToRoleAsync(user, AppRoles.Investor);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        var grant = AddWalletWithGrant(user.Id);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Registered investor {UserId} with initial grant {Grant}", user.Id, grant);

        return (IdentityResult.Success, user);
    }

    public async Task SetPreferredLanguageAsync(string userId, string culture, CancellationToken cancellationToken = default)
    {
        // Chỉ cập nhật một cột, không cần nạp entity; bỏ qua nếu giá trị không đổi.
        var updated = await _context.Users
            .Where(u => u.Id == userId && u.PreferredLanguage != culture)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PreferredLanguage, culture), cancellationToken);
        if (updated > 0)
        {
            _logger.LogInformation("User {UserId} preferred language set to {Culture}.", userId, culture);
        }
    }

    public async Task<bool> EnsureWalletAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (await _context.Wallets.AnyAsync(w => w.UserId == userId, cancellationToken))
        {
            return false;
        }

        var grant = AddWalletWithGrant(userId);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created wallet for user {UserId} with initial grant {Grant}", userId, grant);
        return true;
    }

    /// <summary>Thêm ví mới (chưa SaveChanges) với số dư cấp ban đầu theo cấu hình.</summary>
    private decimal AddWalletWithGrant(string userId)
    {
        var grant = _configuration.GetValue("InitialWalletGrant", DefaultInitialGrant);
        var wallet = new Wallet
        {
            UserId = userId,
            Balance = grant,
            LastUpdatedAt = DateTime.UtcNow
        };
        wallet.Transactions.Add(new WalletTransaction
        {
            Amount = grant,
            BalanceAfter = grant,
            Type = WalletTransactionType.InitialGrant,
            Description = "Khởi tạo tài khoản",
            CreatedAt = DateTime.UtcNow
        });
        _context.Wallets.Add(wallet);
        return grant;
    }
}

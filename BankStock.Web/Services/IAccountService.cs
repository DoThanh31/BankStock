using BankStock.Web.Dtos;
using BankStock.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace BankStock.Web.Services;

public interface IAccountService
{
    /// <summary>
    /// Tạo tài khoản Investor kèm Wallet và giao dịch InitialGrant trong cùng một DB transaction.
    /// </summary>
    Task<(IdentityResult Result, ApplicationUser? User)> RegisterInvestorAsync(RegisterInvestorCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo ví + InitialGrant nếu user chưa có ví (vd. Admin gán thêm role Investor). Trả về true nếu vừa tạo.
    /// </summary>
    Task<bool> EnsureWalletAsync(string userId, CancellationToken cancellationToken = default);
}

using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public sealed record UserAdminListItem(
    string Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTimeOffset? LockoutEnd,
    DateTime CreatedAt,
    decimal? WalletBalance,
    decimal? WalletBlockedBalance)
{
    /// <summary>Bị Identity khóa tạm do nhập sai mật khẩu quá số lần cho phép.</summary>
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public bool IsAdmin => Roles.Contains(AppRoles.Admin);
}

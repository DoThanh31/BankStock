using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountService _accountService;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IAccountService accountService,
        IAuditLogService auditLog,
        ILogger<UserAdminService> logger)
    {
        _context = context;
        _userManager = userManager;
        _accountService = accountService;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<PaginatedList<UserAdminListItem>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var users = _context.Users.AsNoTracking();
        var now = DateTimeOffset.UtcNow;

        if (query.Q is { } keyword)
        {
            users = users.Where(u => (u.Email != null && u.Email.Contains(keyword)) || u.FullName.Contains(keyword));
        }
        if (query.Role is { } role)
        {
            users = users.Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id
                && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }
        users = query.Status switch
        {
            UserStatusFilter.Active => users.Where(u => u.IsActive && (u.LockoutEnd == null || u.LockoutEnd <= now)),
            UserStatusFilter.Disabled => users.Where(u => !u.IsActive),
            UserStatusFilter.LockedOut => users.Where(u => u.LockoutEnd != null && u.LockoutEnd > now),
            _ => users
        };

        var page = await users
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                Roles = (from ur in _context.UserRoles
                         join r in _context.Roles on ur.RoleId equals r.Id
                         where ur.UserId == u.Id
                         orderby r.Name
                         select r.Name).ToList(),
                u.IsActive,
                u.LockoutEnd,
                u.CreatedAt,
                Balance = u.Wallet != null ? (decimal?)u.Wallet.Balance : null,
                Blocked = u.Wallet != null ? (decimal?)u.Wallet.BlockedBalance : null
            })
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);

        return page.Select(u => new UserAdminListItem(
            u.Id, u.Email ?? string.Empty, u.FullName, u.Roles.Where(r => r != null).Select(r => r!).ToList(),
            u.IsActive, u.LockoutEnd, u.CreatedAt, u.Balance, u.Blocked));
    }

    public async Task<UserRolesViewModel?> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsAdmin = roles.Contains(AppRoles.Admin),
            IsInvestor = roles.Contains(AppRoles.Investor)
        };
    }

    public async Task<ServiceResult> SetActiveAsync(string userId, bool isActive, string actorUserId, CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            return ServiceResult.Conflict("UserCannotLockSelf");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.NotFound();
        }

        user.IsActive = isActive;
        IdentityResult result;
        if (isActive)
        {
            // Mở khóa: bỏ luôn lockout tạm thời do nhập sai mật khẩu.
            result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                result = await _userManager.ResetAccessFailedCountAsync(user);
            }
        }
        else
        {
            // Đổi security stamp để cookie đăng nhập hiện tại của user bị vô hiệu ở lần kiểm tra kế tiếp.
            result = await _userManager.UpdateSecurityStampAsync(user);
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to set active={IsActive} for user {UserId}: {Errors}", isActive, userId,
                string.Join("; ", result.Errors.Select(e => e.Code)));
            return ServiceResult.Invalid("AdminOperationFailed");
        }

        await _auditLog.LogAsync(isActive ? "UnlockUser" : "LockUser", actorUserId, nameof(ApplicationUser), user.Id, user.Email, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateRolesAsync(UserRolesViewModel model, string actorUserId, CancellationToken cancellationToken = default)
    {
        if (!model.IsAdmin && !model.IsInvestor)
        {
            return ServiceResult.Invalid("UserRoleRequired");
        }
        if (model.UserId == actorUserId && !model.IsAdmin)
        {
            return ServiceResult.Conflict("UserCannotRemoveOwnAdmin");
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return ServiceResult.NotFound();
        }

        // Thông tin hiển thị lấy từ DB, không tin giá trị hidden field gửi lên.
        model.Email = user.Email ?? string.Empty;
        model.FullName = user.FullName;

        var current = await _userManager.GetRolesAsync(user);
        var desired = new List<string>();
        if (model.IsAdmin) desired.Add(AppRoles.Admin);
        if (model.IsInvestor) desired.Add(AppRoles.Investor);

        var toAdd = desired.Except(current).ToList();
        var toRemove = current.Except(desired).ToList();
        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return ServiceResult.Success();
        }

        // UserManager dùng chung AppDbContext (scoped) nên transaction bao cả role và ví.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var result = toAdd.Count > 0 ? await _userManager.AddToRolesAsync(user, toAdd) : IdentityResult.Success;
        if (result.Succeeded && toRemove.Count > 0)
        {
            result = await _userManager.RemoveFromRolesAsync(user, toRemove);
        }
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update roles for user {UserId}: {Errors}", user.Id, string.Join("; ", result.Errors.Select(e => e.Code)));
            return ServiceResult.Invalid("AdminOperationFailed");
        }

        if (toAdd.Contains(AppRoles.Investor))
        {
            await _accountService.EnsureWalletAsync(user.Id, cancellationToken);
        }

        // Role nằm trong cookie claims: đổi stamp để cookie được làm mới ở lần kiểm tra kế tiếp.
        await _userManager.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync(cancellationToken);

        await _auditLog.LogAsync("UpdateUserRoles", actorUserId, nameof(ApplicationUser), user.Id,
            $"{user.Email}: {string.Join(",", current)} -> {string.Join(",", desired)}", cancellationToken);
        return ServiceResult.Success();
    }
}

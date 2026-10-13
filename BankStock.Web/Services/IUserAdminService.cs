using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>Quản lý tài khoản người dùng: khóa/mở khóa và gán role.</summary>
public interface IUserAdminService
{
    Task<PaginatedList<UserAdminListItem>> GetPagedAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<UserRolesViewModel?> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Khóa (IsActive = false, buộc đăng xuất) hoặc mở khóa (xóa cả lockout do sai mật khẩu). Không cho tự khóa mình.</summary>
    Task<ServiceResult> SetActiveAsync(string userId, bool isActive, string actorUserId, CancellationToken cancellationToken = default);

    /// <summary>Gán role Admin/Investor; thêm Investor thì tự tạo ví nếu chưa có. Không cho tự gỡ quyền Admin của mình.</summary>
    Task<ServiceResult> UpdateRolesAsync(UserRolesViewModel model, string actorUserId, CancellationToken cancellationToken = default);
}

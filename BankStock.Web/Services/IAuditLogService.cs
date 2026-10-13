using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string? userId, string? entity = null, string? entityId = null, string? detail = null, CancellationToken cancellationToken = default);

    /// <summary>Ghi log cho người dùng đang đăng nhập (lấy UserId từ HttpContext).</summary>
    Task LogCurrentUserAsync(string action, string? entity = null, string? entityId = null, string? detail = null, CancellationToken cancellationToken = default);

    Task<PaginatedList<AuditLogListItem>> GetPagedAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);

    /// <summary>Các loại hành động đã từng ghi log (cho ô lọc).</summary>
    Task<List<string>> GetActionNamesAsync(CancellationToken cancellationToken = default);

    Task<List<AuditLogListItem>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}

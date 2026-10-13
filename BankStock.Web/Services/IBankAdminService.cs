using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>CRUD ngân hàng cho khu Admin.</summary>
public interface IBankAdminService
{
    Task<PaginatedList<BankAdminListItem>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken = default);
    /// <summary>Một ngân hàng kèm số mã cổ phiếu; null nếu không tồn tại.</summary>
    Task<BankAdminListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BankFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(BankFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(BankFormViewModel model, CancellationToken cancellationToken = default);

    /// <summary>Chặn xóa khi ngân hàng còn mã cổ phiếu (trả về Conflict).</summary>
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<List<BankOptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default);
}

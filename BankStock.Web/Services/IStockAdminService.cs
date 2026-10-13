using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>CRUD mã cổ phiếu và chỉ số cơ bản cho khu Admin.</summary>
public interface IStockAdminService
{
    Task<PaginatedList<StockAdminListItem>> GetPagedAsync(StockAdminListQuery query, CancellationToken cancellationToken = default);
    Task<StockFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(StockFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(StockFormViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa mã kèm lịch sử giá và BCTC. Chặn (Conflict) nếu mã đã phát sinh lệnh, giao dịch, danh mục hoặc watchlist.
    /// </summary>
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<List<StockOptionItem>> GetOptionsAsync(CancellationToken cancellationToken = default);
}

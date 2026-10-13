using BankStock.Web.Dtos;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Services;

/// <summary>CRUD lịch sử giá và điều chỉnh giá thủ công (tham chiếu, trần, sàn, giá khớp).</summary>
public interface IPriceHistoryAdminService
{
    Task<PaginatedList<PriceHistoryListItem>> GetPagedAsync(PriceHistoryListQuery query, CancellationToken cancellationToken = default);
    Task<PriceHistoryFormViewModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(PriceHistoryFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(PriceHistoryFormViewModel model, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dữ liệu form điều chỉnh giá cho một ngày (mặc định hôm nay). Ngày chưa có phiên thì gợi ý
    /// giá tham chiếu = giá đóng cửa phiên trước, trần/sàn theo biên độ sàn. Trả về null nếu mã không tồn tại.
    /// </summary>
    Task<PriceAdjustViewModel?> GetAdjustAsync(int stockId, DateTime? tradeDate, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật (hoặc tạo) phiên giá theo giá tham chiếu/trần/sàn/khớp do Admin nhập.</summary>
    Task<ServiceResult> AdjustAsync(PriceAdjustViewModel model, CancellationToken cancellationToken = default);

    /// <summary>Trần/sàn gợi ý theo sàn niêm yết của mã; null nếu mã không tồn tại.</summary>
    Task<PriceBandDto?> GetBandAsync(int stockId, decimal refPrice, CancellationToken cancellationToken = default);
}

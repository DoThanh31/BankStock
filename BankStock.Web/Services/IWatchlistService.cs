using BankStock.Web.Dtos;

namespace BankStock.Web.Services;

/// <summary>Danh sách mã theo dõi của Investor (unique theo UserId + StockId).</summary>
public interface IWatchlistService
{
    /// <summary>Các mã đang theo dõi kèm giá mới nhất, theo thứ tự thêm vào.</summary>
    Task<List<WatchlistItemDto>> GetItemsAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(string userId, string stockCode, CancellationToken cancellationToken = default);

    /// <summary>Ghim / bỏ ghim một mã; trả về trạng thái sau khi đổi (true = đang theo dõi).</summary>
    Task<(ServiceResult Result, bool InWatchlist)> ToggleAsync(string userId, string stockCode, CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveAsync(string userId, string stockCode, CancellationToken cancellationToken = default);
}

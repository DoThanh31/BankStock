using BankStock.Web.Dtos;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Services;

public interface IStockService
{
    Task<List<StockBoardItemViewModel>> GetTopStockBoardAsync(CancellationToken cancellationToken = default);
    Task<MarketOverviewViewModel> GetMarketOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>Danh sách cổ phiếu có lọc, sắp xếp và phân trang phía server.</summary>
    Task<PaginatedList<StockListItemViewModel>> GetStockListAsync(StockListQuery query, CancellationToken cancellationToken = default);

    /// <summary>Chi tiết một mã; trả về null nếu không tồn tại hoặc ngừng giao dịch.</summary>
    Task<StockDetailViewModel?> GetStockDetailAsync(string stockCode, CancellationToken cancellationToken = default);

    /// <summary>Dữ liệu nến theo khoảng thời gian; trả về null nếu mã không tồn tại.</summary>
    Task<StockHistoryDto?> GetPriceHistoryAsync(string stockCode, PriceTimeframe timeframe, CancellationToken cancellationToken = default);

    /// <summary>Giá mới nhất của các mã cho trước, giữ nguyên thứ tự truyền vào.</summary>
    Task<List<StockBoardItemViewModel>> GetBoardItemsByCodesAsync(IReadOnlyList<string> stockCodes, CancellationToken cancellationToken = default);
}

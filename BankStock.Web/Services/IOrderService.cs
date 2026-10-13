using BankStock.Web.Dtos;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Services;

/// <summary>
/// Đặt / hủy / khớp lệnh giả lập. Mọi thao tác của Investor lọc theo <c>userId</c> (chống IDOR) và chạy trong
/// DB transaction; Wallet/Holding dùng RowVersion và được thử lại khi bị cập nhật đồng thời.
/// </summary>
public interface IOrderService
{
    /// <summary>Các mã đang giao dịch (cho ô chọn mã ở form đặt lệnh).</summary>
    Task<List<StockOptionItem>> GetTradableStocksAsync(CancellationToken cancellationToken = default);

    /// <summary>Giá phiên gần nhất + tiền/cổ phiếu khả dụng của người dùng; null nếu mã không giao dịch.</summary>
    Task<TradeQuoteDto?> GetQuoteAsync(string userId, string stockCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt lệnh: kiểm tra lô, giá trong biên độ sàn–trần, đủ tiền (mua) / đủ cổ phiếu (bán), giam tiền hoặc cổ phiếu.
    /// Lệnh Market khớp ngay theo giá khớp hiện tại; lệnh Limit khớp ngay nếu giá hiện tại đã thỏa, ngược lại chờ.
    /// </summary>
    Task<(ServiceResult Result, OrderPlacedDto? Order)> PlaceOrderAsync(string userId, PlaceOrderCommand command, CancellationToken cancellationToken = default);

    /// <summary>Hủy lệnh Pending của chính người dùng và hoàn lại tiền / cổ phiếu bị giam.</summary>
    Task<ServiceResult> CancelOrderAsync(string userId, int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khớp các lệnh Limit đang chờ của một mã khi giá khớp thay đổi (Admin điều chỉnh giá):
    /// lệnh mua có giá ≥ giá khớp, lệnh bán có giá ≤ giá khớp; khớp theo thứ tự thời gian đặt. Trả về số lệnh đã khớp.
    /// </summary>
    Task<int> MatchPendingOrdersAsync(int stockId, decimal matchPrice, CancellationToken cancellationToken = default);

    Task<PaginatedList<OrderListItem>> GetOrdersAsync(string userId, OrderListQuery query, CancellationToken cancellationToken = default);

    /// <summary>Một lệnh của chính người dùng; null nếu không tồn tại hoặc thuộc người khác (chống IDOR).</summary>
    Task<OrderListItem?> GetOrderAsync(string userId, int orderId, CancellationToken cancellationToken = default);

    Task<PaginatedList<TradeListItem>> GetTradesAsync(string userId, TradeListQuery query, CancellationToken cancellationToken = default);
}

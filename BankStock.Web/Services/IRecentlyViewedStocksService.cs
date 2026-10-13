namespace BankStock.Web.Services;

/// <summary>Danh sách mã cổ phiếu người dùng vừa xem, lưu trong Session.</summary>
public interface IRecentlyViewedStocksService
{
    IReadOnlyList<string> GetCodes();
    void Add(string stockCode);
}

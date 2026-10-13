namespace BankStock.Web.ViewModels.Admin;

public sealed record PriceHistoryListItem(
    int Id,
    int StockId,
    string StockCode,
    DateTime TradeDate,
    decimal RefPrice,
    decimal CeilingPrice,
    decimal FloorPrice,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume)
{
    /// <summary>Lớp màu theo quy ước sàn VN (trần/sàn/tăng/giảm/tham chiếu).</summary>
    public string StatusClass =>
        Close >= CeilingPrice ? "stock-ceil"
        : Close <= FloorPrice ? "stock-floor"
        : Close > RefPrice ? "stock-up"
        : Close < RefPrice ? "stock-down"
        : "stock-ref";
}

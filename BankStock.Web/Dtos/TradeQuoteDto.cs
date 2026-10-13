namespace BankStock.Web.Dtos;

/// <summary>Thông tin cho form đặt lệnh: giá phiên gần nhất, tiền và cổ phiếu khả dụng của người dùng.</summary>
public record TradeQuoteDto(
    string StockCode,
    string BankShortName,
    string Exchange,
    DateTime? TradeDate,
    decimal RefPrice,
    decimal CeilingPrice,
    decimal FloorPrice,
    decimal MatchPrice,
    decimal AvailableCash,
    int AvailableQuantity,
    decimal FeePercent,
    int LotSize);

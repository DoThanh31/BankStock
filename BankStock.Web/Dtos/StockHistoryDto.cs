namespace BankStock.Web.Dtos;

public record StockHistoryDto(string StockCode, string Timeframe, IReadOnlyList<PriceCandleDto> Candles);

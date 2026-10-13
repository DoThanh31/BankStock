namespace BankStock.Web.Dtos;

public record WatchlistItemDto(StockQuoteDto Quote, DateTime AddedAt);

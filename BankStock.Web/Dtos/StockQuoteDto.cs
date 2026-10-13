using BankStock.Web.Models;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Giá mới nhất của một mã (dùng cho API bảng giá và watchlist).</summary>
public record StockQuoteDto(
    string StockCode,
    string BankShortName,
    string Exchange,
    BankGroup BankGroup,
    decimal RefPrice,
    decimal CeilingPrice,
    decimal FloorPrice,
    decimal CurrentPrice,
    decimal Change,
    decimal ChangePercent,
    long Volume,
    StockPriceStatus Status)
{
    public static StockQuoteDto From(StockBoardItemViewModel item) => new(
        item.StockCode, item.BankShortName, item.Exchange, item.BankGroup,
        item.RefPrice, item.CeilingPrice, item.FloorPrice, item.CurrentPrice,
        item.Change, item.ChangePercent, item.Volume, item.Status);
}

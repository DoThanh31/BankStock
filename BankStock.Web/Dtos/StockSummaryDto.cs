using BankStock.Web.Models;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Một mã trong danh sách API: giá phiên gần nhất + chỉ số cơ bản dùng để lọc.</summary>
public record StockSummaryDto(
    string StockCode,
    string BankName,
    string BankShortName,
    string Exchange,
    BankGroup BankGroup,
    decimal RefPrice,
    decimal CeilingPrice,
    decimal FloorPrice,
    decimal CurrentPrice,
    decimal High,
    decimal Low,
    decimal Change,
    decimal ChangePercent,
    long Volume,
    StockPriceStatus Status,
    decimal? PE,
    decimal? PB,
    decimal? CASA,
    decimal? DividendYield,
    long ListedShares,
    decimal MarketCap)
{
    public static StockSummaryDto From(StockListItemViewModel item) => new(
        item.StockCode, item.BankName, item.BankShortName, item.Exchange, item.BankGroup,
        item.RefPrice, item.CeilingPrice, item.FloorPrice, item.CurrentPrice, item.High, item.Low,
        item.Change, item.ChangePercent, item.Volume, item.Status,
        item.PE, item.PB, item.CASA, item.DividendYield, item.ListedShares, item.MarketCap);
}

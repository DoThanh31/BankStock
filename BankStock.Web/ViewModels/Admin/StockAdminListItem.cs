using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public sealed record StockAdminListItem(
    int Id,
    string StockCode,
    string BankShortName,
    string Exchange,
    BankGroup BankGroup,
    bool IsActive,
    decimal? LastClose,
    DateTime? LastTradeDate,
    decimal? PE,
    decimal? CASA,
    int PriceCount);

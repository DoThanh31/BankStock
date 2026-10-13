namespace BankStock.Web.ViewModels;

public sealed record StockOptionItem(int Id, string StockCode, string Exchange, bool IsActive);

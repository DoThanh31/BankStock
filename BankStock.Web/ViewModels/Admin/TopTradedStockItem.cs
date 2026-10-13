namespace BankStock.Web.ViewModels.Admin;

/// <summary>Một dòng xếp hạng mã giao dịch nhiều nhất (theo tổng khối lượng trong kỳ).</summary>
public sealed record TopTradedStockItem(string StockCode, string BankShortName, long Volume, decimal Value, decimal SharePercent);

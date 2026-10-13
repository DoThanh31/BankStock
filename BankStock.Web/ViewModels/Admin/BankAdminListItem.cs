namespace BankStock.Web.ViewModels.Admin;

public sealed record BankAdminListItem(
    int Id,
    string Code,
    string Name,
    string ShortName,
    int? Founded,
    string? Headquarters,
    string? Website,
    string? LogoUrl,
    string? Description,
    int StockCount);

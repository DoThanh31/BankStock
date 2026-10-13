using BankStock.Web.ViewModels.Admin;

namespace BankStock.Web.Dtos;

/// <summary>Ngân hàng và số mã cổ phiếu đang niêm yết.</summary>
public record BankDto(
    int Id,
    string Code,
    string Name,
    string ShortName,
    int? Founded,
    string? Headquarters,
    string? Website,
    string? LogoUrl,
    string? Description,
    int StockCount)
{
    public static BankDto From(BankAdminListItem item) => new(
        item.Id, item.Code, item.Name, item.ShortName, item.Founded, item.Headquarters,
        item.Website, item.LogoUrl, item.Description, item.StockCount);
}

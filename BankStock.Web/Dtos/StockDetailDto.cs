using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Chi tiết một mã: giá + chỉ số, thông tin ngân hàng phát hành và BCTC các quý gần nhất.</summary>
public record StockDetailDto(
    StockSummaryDto Summary,
    DateTime? LastTradeDate,
    decimal FaceValue,
    decimal? EPS,
    decimal? NPL,
    decimal? CharterCapital,
    int? Founded,
    string? Headquarters,
    string? Website,
    string? Description,
    IReadOnlyList<FinancialReportDto> FinancialReports)
{
    public static StockDetailDto From(StockDetailViewModel detail) => new(
        StockSummaryDto.From(detail.Quote), detail.LastTradeDate, detail.FaceValue,
        detail.EPS, detail.NPL, detail.CharterCapital,
        detail.Founded, detail.Headquarters, detail.Website, detail.Description,
        detail.FinancialReports.Select(FinancialReportDto.From).ToList());
}

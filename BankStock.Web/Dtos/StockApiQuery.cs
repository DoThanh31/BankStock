using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Bộ lọc của GET /api/v1/stocks (cùng tiêu chí với trang danh sách cổ phiếu).</summary>
public record StockApiQuery : ApiPageQuery
{
    [StringLength(50, ErrorMessage = "ValMaxLength")]
    public string? Q { get; init; }

    public BankGroup? Group { get; init; }

    public bool PeUnder10 { get; init; }

    public bool CasaOver30 { get; init; }

    [Range(0, 100, ErrorMessage = "ValRange")]
    public decimal? MinDividend { get; init; }

    public StockSortField Sort { get; init; } = StockSortField.Code;

    public SortDirection Dir { get; init; } = SortDirection.Asc;

    public StockListQuery ToListQuery() => new()
    {
        Q = Q,
        Group = Group,
        PeUnder10 = PeUnder10,
        CasaOver30 = CasaOver30,
        MinDividend = MinDividend,
        Sort = Sort,
        Dir = Dir,
        Page = Page,
        PageSize = PageSize
    };
}

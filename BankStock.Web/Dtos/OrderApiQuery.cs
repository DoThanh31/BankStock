using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;
using BankStock.Web.ViewModels.Investor;

namespace BankStock.Web.Dtos;

/// <summary>Bộ lọc của GET /api/v1/orders: mã cổ phiếu, trạng thái, loại lệnh.</summary>
public record OrderApiQuery : ApiPageQuery
{
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "ValStockCode")]
    public string? StockCode { get; init; }

    public OrderStatus? Status { get; init; }

    public OrderType? Type { get; init; }

    public OrderListQuery ToListQuery() => new()
    {
        Q = StockCode?.ToUpperInvariant(),
        Status = Status,
        Type = Type,
        Page = Page,
        PageSize = PageSize
    };
}

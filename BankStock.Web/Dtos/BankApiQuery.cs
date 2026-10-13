using System.ComponentModel.DataAnnotations;
using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Bộ lọc của GET /api/v1/banks: tìm theo mã / tên / tên viết tắt.</summary>
public record BankApiQuery : ApiPageQuery
{
    [StringLength(100, ErrorMessage = "ValMaxLength")]
    public string? Q { get; init; }

    public PagedListQuery ToListQuery() => new() { Q = Q, Page = Page, PageSize = PageSize };
}

using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.Dtos;

/// <summary>Tham số phân trang chung của API: <c>?page=&amp;pageSize=</c> (pageSize tối đa 100).</summary>
public record ApiPageQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "ValRange")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "ValRange")]
    public int PageSize { get; init; } = 20;
}

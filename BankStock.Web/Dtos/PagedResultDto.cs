using BankStock.Web.ViewModels;

namespace BankStock.Web.Dtos;

/// <summary>Một trang kết quả của API kèm thông tin phân trang.</summary>
public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages)
{
    public static PagedResultDto<T> From<TSource>(PaginatedList<TSource> source, Func<TSource, T> map) =>
        new(source.Items.Select(map).ToList(), source.PageIndex, source.PageSize, source.TotalCount, source.TotalPages);
}

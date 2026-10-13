using BankStock.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public static class PagingExtensions
{
    /// <summary>
    /// Phân trang phía server bằng Skip/Take. Trang vượt quá tổng số trang được đưa về trang cuối.
    /// </summary>
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        int totalCount = await source.CountAsync(cancellationToken);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, page, pageSize);
    }
}

using BankStock.Web.Data;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using BankStock.Web.ViewModels.Investor;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class OrderAdminService : IOrderAdminService
{
    private readonly AppDbContext _context;

    public OrderAdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<AdminOrderListItem>> GetPagedAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var orders = _context.Orders.AsNoTracking();

        if (query.StockId is { } stockId) orders = orders.Where(o => o.StockId == stockId);
        if (query.Status is { } status) orders = orders.Where(o => o.Status == status);
        if (query.Type is { } type) orders = orders.Where(o => o.Type == type);
        if (query.Q is { } keyword)
        {
            orders = orders.Where(o => o.Stock!.StockCode.Contains(keyword)
                || (o.User!.Email != null && o.User.Email.Contains(keyword)));
        }

        return await orders
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Select(o => new AdminOrderListItem(
                o.Id,
                o.User!.Email ?? string.Empty,
                o.Stock!.StockCode,
                o.Type, o.Mode, o.Status, o.Quantity, o.Price,
                _context.Trades.Where(t => t.OrderId == o.Id).Select(t => (decimal?)t.Price).FirstOrDefault(),
                o.Fee,
                o.CreatedAt))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }
}

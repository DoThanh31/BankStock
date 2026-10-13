using System.Security.Claims;
using BankStock.Web.Data;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace BankStock.Web.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string? userId, string? entity = null, string? entityId = null, string? detail = null, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            Entity = entity,
            EntityId = entityId,
            Detail = detail,
            CreatedAt = DateTime.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task LogCurrentUserAsync(string action, string? entity = null, string? entityId = null, string? detail = null, CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return LogAsync(action, userId, entity, entityId, detail, cancellationToken);
    }

    public async Task<PaginatedList<AuditLogListItem>> GetPagedAsync(AuditLogListQuery query, CancellationToken cancellationToken = default)
    {
        query.Normalize();
        var logs = _context.AuditLogs.AsNoTracking();

        if (query.Q is { } keyword)
        {
            logs = logs.Where(l => (l.Detail != null && l.Detail.Contains(keyword))
                || (l.EntityId != null && l.EntityId.Contains(keyword))
                || (l.IpAddress != null && l.IpAddress.Contains(keyword))
                || (l.User != null && l.User.Email != null && l.User.Email.Contains(keyword)));
        }
        if (query.LogAction is { } action)
        {
            logs = logs.Where(l => l.Action == action);
        }
        if (query.From is { } from)
        {
            var fromDate = from.Date;
            logs = logs.Where(l => l.CreatedAt >= fromDate);
        }
        if (query.To is { } to)
        {
            var toExclusive = to.Date.AddDays(1);
            logs = logs.Where(l => l.CreatedAt < toExclusive);
        }

        return await Project(logs.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id))
            .ToPaginatedListAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<List<string>> GetActionNamesAsync(CancellationToken cancellationToken = default) =>
        _context.AuditLogs
            .AsNoTracking()
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);

    public Task<List<AuditLogListItem>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        Project(_context.AuditLogs.AsNoTracking().OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id))
            .Take(count)
            .ToListAsync(cancellationToken);

    private static IQueryable<AuditLogListItem> Project(IQueryable<AuditLog> logs) =>
        logs.Select(l => new AuditLogListItem(
            l.Id,
            l.CreatedAt,
            l.Action,
            l.User != null ? l.User.Email : null,
            l.Entity,
            l.EntityId,
            l.Detail,
            l.IpAddress));
}

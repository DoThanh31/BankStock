namespace BankStock.Web.ViewModels.Admin;

public sealed record AuditLogListItem(
    int Id,
    DateTime CreatedAt,
    string Action,
    string? UserEmail,
    string? Entity,
    string? EntityId,
    string? Detail,
    string? IpAddress);

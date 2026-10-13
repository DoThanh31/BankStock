using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class AuditLogsController : AdminControllerBase
{
    private readonly IAuditLogService _auditLog;

    public AuditLogsController(IAuditLogService auditLog, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _auditLog = auditLog;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] AuditLogListQuery query, CancellationToken cancellationToken)
    {
        var items = await _auditLog.GetPagedAsync(query, cancellationToken);
        return ListView(new PagedListViewModel<AuditLogListItem, AuditLogListQuery>(query, items)
        {
            FilterOptions = await _auditLog.GetActionNamesAsync(cancellationToken)
        });
    }
}

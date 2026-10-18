using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

public class AuditLogListQuery : PagedListQuery
{
    // Không đặt tên "Action" vì trùng route value {action} của MVC.
    [StringLength(100, ErrorMessage = "ValMaxLength")]
    [Display(Name = "FieldAuditAction")]
    public string? LogAction { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldFromDate")]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldToDate")]
    public DateTime? To { get; set; }

    public override bool HasFilters => base.HasFilters || LogAction is not null || From.HasValue || To.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        LogAction = string.IsNullOrWhiteSpace(LogAction) ? null : LogAction.Trim();
        if (From > To) (From, To) = (To, From);
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (LogAction is not null) values["logAction"] = LogAction;
        if (From.HasValue) values["from"] = FormatDate(From.Value);
        if (To.HasValue) values["to"] = FormatDate(To.Value);
    }
}

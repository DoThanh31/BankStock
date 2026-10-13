using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public class StockAdminListQuery : PagedListQuery
{
    [Display(Name = "FieldBankGroup")]
    public BankGroup? Group { get; set; }

    [Display(Name = "FieldExchange")]
    public string? Exchange { get; set; }

    [Display(Name = "FieldStatus")]
    public bool? Active { get; set; }

    public override bool HasFilters => base.HasFilters || Group.HasValue || !string.IsNullOrEmpty(Exchange) || Active.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        Exchange = string.IsNullOrWhiteSpace(Exchange) ? null : Exchange.Trim().ToUpperInvariant();
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (Group.HasValue) values["group"] = Group.Value.ToString();
        if (!string.IsNullOrEmpty(Exchange)) values["exchange"] = Exchange;
        if (Active.HasValue) values["active"] = Active.Value ? "true" : "false";
    }
}

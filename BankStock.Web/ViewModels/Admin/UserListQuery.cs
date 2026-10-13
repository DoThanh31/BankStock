using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Admin;

public class UserListQuery : PagedListQuery
{
    public static readonly string[] AllowedRoles = [AppRoles.Admin, AppRoles.Investor];

    [Display(Name = "FieldRole")]
    public string? Role { get; set; }

    [Display(Name = "FieldStatus")]
    public UserStatusFilter? Status { get; set; }

    public override bool HasFilters => base.HasFilters || Role is not null || Status.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        Role = AllowedRoles.FirstOrDefault(r => string.Equals(r, Role, StringComparison.OrdinalIgnoreCase));
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (Role is not null) values["role"] = Role;
        if (Status.HasValue) values["status"] = Status.Value.ToString();
    }
}

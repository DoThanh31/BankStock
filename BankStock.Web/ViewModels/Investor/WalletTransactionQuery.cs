using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

public class WalletTransactionQuery : PagedListQuery
{
    [Display(Name = "AdminTxType")]
    public WalletTransactionType? Type { get; set; }

    public override bool HasFilters => base.HasFilters || Type.HasValue;

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (Type.HasValue) values["type"] = Type.Value.ToString();
    }
}

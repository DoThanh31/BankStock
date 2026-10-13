using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

public class OrderListQuery : PagedListQuery
{
    [Display(Name = "FieldStockCode")]
    public int? StockId { get; set; }

    [Display(Name = "FieldStatus")]
    public OrderStatus? Status { get; set; }

    [Display(Name = "FieldOrderType")]
    public OrderType? Type { get; set; }

    public override bool HasFilters => base.HasFilters || StockId.HasValue || Status.HasValue || Type.HasValue;

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (StockId.HasValue) values["stockId"] = StockId.Value.ToString(CultureInfo.InvariantCulture);
        if (Status.HasValue) values["status"] = Status.Value.ToString();
        if (Type.HasValue) values["type"] = Type.Value.ToString();
    }
}

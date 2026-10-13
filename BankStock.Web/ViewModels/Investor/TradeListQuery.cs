using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BankStock.Web.Models;

namespace BankStock.Web.ViewModels.Investor;

public class TradeListQuery : PagedListQuery
{
    [Display(Name = "FieldStockCode")]
    public int? StockId { get; set; }

    [Display(Name = "FieldOrderType")]
    public OrderType? Type { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldFromDate")]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldToDate")]
    public DateTime? To { get; set; }

    public override bool HasFilters => base.HasFilters || StockId.HasValue || Type.HasValue || From.HasValue || To.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        if (From > To) (From, To) = (To, From);
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (StockId.HasValue) values["stockId"] = StockId.Value.ToString(CultureInfo.InvariantCulture);
        if (Type.HasValue) values["type"] = Type.Value.ToString();
        if (From.HasValue) values["from"] = FormatDate(From.Value);
        if (To.HasValue) values["to"] = FormatDate(To.Value);
    }
}

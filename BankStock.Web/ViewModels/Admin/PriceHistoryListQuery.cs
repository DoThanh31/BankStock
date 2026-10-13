using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BankStock.Web.ViewModels.Admin;

public class PriceHistoryListQuery : PagedListQuery
{
    [Display(Name = "FieldStockCode")]
    public int? StockId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldFromDate")]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "FieldToDate")]
    public DateTime? To { get; set; }

    public override bool HasFilters => base.HasFilters || StockId.HasValue || From.HasValue || To.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        if (From > To) (From, To) = (To, From);
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (StockId.HasValue) values["stockId"] = StockId.Value.ToString(CultureInfo.InvariantCulture);
        if (From.HasValue) values["from"] = FormatDate(From.Value);
        if (To.HasValue) values["to"] = FormatDate(To.Value);
    }
}

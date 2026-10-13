using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BankStock.Web.ViewModels.Admin;

public class FinancialReportListQuery : PagedListQuery
{
    [Display(Name = "FieldStockCode")]
    public int? StockId { get; set; }

    [Display(Name = "FieldYear")]
    public int? Year { get; set; }

    public override bool HasFilters => base.HasFilters || StockId.HasValue || Year.HasValue;

    public override void Normalize()
    {
        base.Normalize();
        if (Year is < 2000 or > 2100) Year = null;
    }

    protected override void AddFilterValues(Dictionary<string, string> values)
    {
        if (StockId.HasValue) values["stockId"] = StockId.Value.ToString(CultureInfo.InvariantCulture);
        if (Year.HasValue) values["year"] = Year.Value.ToString(CultureInfo.InvariantCulture);
    }
}

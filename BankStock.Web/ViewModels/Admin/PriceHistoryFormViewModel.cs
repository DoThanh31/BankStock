using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>Form CRUD đầy đủ một phiên giá (OHLC + trần/sàn/tham chiếu).</summary>
public class PriceHistoryFormViewModel : PriceBandFormBase
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldStockCode")]
    public int StockId { get; set; }

    [Required(ErrorMessage = "ValRequired")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "FieldTradeDate")]
    public DateTime? TradeDate { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [Display(Name = "ThOpen")]
    public decimal Open { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [Display(Name = "ThHigh")]
    public decimal High { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [Display(Name = "ThLow")]
    public decimal Low { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [Display(Name = "ThClose")]
    public decimal Close { get; set; }

    public bool IsNew => Id == 0;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (High < Math.Max(Open, Close) || High < Low)
        {
            yield return new ValidationResult(Localize(validationContext, "ValHighTooLow"), [nameof(High)]);
        }
        if (Low > Math.Min(Open, Close))
        {
            yield return new ValidationResult(Localize(validationContext, "ValLowTooHigh"), [nameof(Low)]);
        }
        if (High > CeilingPrice || Low < FloorPrice)
        {
            yield return new ValidationResult(Localize(validationContext, "ValPriceOutOfBand"), [nameof(High)]);
        }
    }
}

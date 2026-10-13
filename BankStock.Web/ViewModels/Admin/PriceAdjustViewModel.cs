using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>
/// Điều chỉnh giá thủ công một phiên: giá tham chiếu, trần, sàn và giá khớp hiện tại.
/// Ngày chưa có phiên thì tạo mới (Open = High = Low = Close = giá khớp).
/// </summary>
public class PriceAdjustViewModel : PriceBandFormBase
{
    public int StockId { get; set; }

    public string StockCode { get; set; } = string.Empty;

    public string Exchange { get; set; } = string.Empty;

    /// <summary>true nếu ngày đang chọn chưa có phiên giá (sẽ tạo mới).</summary>
    public bool IsNewSession { get; set; }

    [Required(ErrorMessage = "ValRequired")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "FieldTradeDate")]
    public DateTime? TradeDate { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    [Display(Name = "ThMatchPrice")]
    public decimal MatchPrice { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (MatchPrice > CeilingPrice || MatchPrice < FloorPrice)
        {
            yield return new ValidationResult(Localize(validationContext, "ValMatchOutOfBand"), [nameof(MatchPrice)]);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.ViewModels.Admin;

/// <summary>Ràng buộc chung cho các form nhập giá: Sàn ≤ Tham chiếu ≤ Trần.</summary>
public abstract class PriceBandFormBase : IValidatableObject
{
    public const double MaxPrice = 10_000_000;

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "ThRef")]
    public decimal RefPrice { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "ThCeiling")]
    public decimal CeilingPrice { get; set; }

    [Range(1, MaxPrice, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "ThFloor")]
    public decimal FloorPrice { get; set; }

    [Range(0, 1_000_000_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "ThVolume")]
    public long Volume { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FloorPrice > RefPrice)
        {
            yield return new ValidationResult(Localize(validationContext, "ValFloorAboveRef"), [nameof(FloorPrice)]);
        }
        if (CeilingPrice < RefPrice)
        {
            yield return new ValidationResult(Localize(validationContext, "ValCeilingBelowRef"), [nameof(CeilingPrice)]);
        }
    }

    /// <summary>Thông báo lỗi đa ngôn ngữ; ValidationContext của MVC cung cấp RequestServices.</summary>
    protected static string Localize(ValidationContext context, string key) =>
        context.GetService(typeof(IStringLocalizer<SharedResource>)) is IStringLocalizer<SharedResource> localizer
            ? localizer[key].Value
            : key;
}

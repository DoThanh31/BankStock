using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.Validation;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.ViewModels.Investor;

/// <summary>Form đặt lệnh mua/bán. Service kiểm tra lại toàn bộ (giá trong biên độ, đủ tiền/cổ phiếu).</summary>
public class PlaceOrderViewModel : IValidatableObject
{
    [Required(ErrorMessage = "ValRequired")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "ValStockCode")]
    [Display(Name = "FieldStockCode")]
    public string StockCode { get; set; } = string.Empty;

    [EnumDataType(typeof(OrderType), ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldOrderType")]
    public OrderType Type { get; set; } = OrderType.Buy;

    [EnumDataType(typeof(OrderMode), ErrorMessage = "ValSelectRequired")]
    [Display(Name = "FieldOrderMode")]
    public OrderMode Mode { get; set; } = OrderMode.Market;

    [Range(TradingRules.LotSize, TradingRules.MaxOrderQuantity, ErrorMessage = "ValQuantityRange")]
    [LotSize(TradingRules.LotSize, ErrorMessage = "ValLotSize")]
    [DisplayFormat(DataFormatString = "{0:N0}")]
    [Display(Name = "FieldQuantity")]
    public int Quantity { get; set; } = TradingRules.LotSize;

    [Range(1, 10_000_000, ErrorMessage = "ValRange")]
    [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
    [Display(Name = "FieldOrderPrice")]
    public decimal? Price { get; set; }

    /// <summary>Lệnh nháp đang được đặt (nếu có): đặt thành công thì xóa khỏi giỏ lệnh nháp.</summary>
    public Guid? DraftId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Mode == OrderMode.Limit && Price is null)
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
            yield return new ValidationResult(localizer?["ValLimitPriceRequired"].Value ?? "ValLimitPriceRequired", [nameof(Price)]);
        }
    }
}

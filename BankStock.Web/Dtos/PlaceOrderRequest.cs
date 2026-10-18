using System.ComponentModel.DataAnnotations;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.Validation;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Dtos;

/// <summary>
/// Body của POST /api/v1/orders. <c>Price</c> bắt buộc với lệnh Limit, bị bỏ qua với lệnh Market.
/// Service kiểm tra lại toàn bộ (lô, biên độ giá, đủ tiền / cổ phiếu). <c>[Display]</c> dùng cùng khóa resource với
/// <see cref="ViewModels.Investor.PlaceOrderViewModel"/> để lỗi validation của API hiện tên trường theo ngôn ngữ.
/// </summary>
public record PlaceOrderRequest(
    [Display(Name = "FieldStockCode"), Required(ErrorMessage = "ValRequired"), RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "ValStockCode")] string StockCode,
    [Display(Name = "FieldOrderType"), EnumDataType(typeof(OrderType), ErrorMessage = "ValSelectRequired")] OrderType Type,
    [Display(Name = "FieldOrderMode"), EnumDataType(typeof(OrderMode), ErrorMessage = "ValSelectRequired")] OrderMode Mode,
    [Display(Name = "FieldQuantity"), Range(TradingRules.LotSize, TradingRules.MaxOrderQuantity, ErrorMessage = "ValQuantityRange"), LotSize(TradingRules.LotSize, ErrorMessage = "ValLotSize")] int Quantity,
    [Display(Name = "FieldOrderPrice"), Range(1, 10_000_000, ErrorMessage = "ValRange")] decimal? Price) : IValidatableObject
{
    public PlaceOrderCommand ToCommand() =>
        new(StockCode.ToUpperInvariant(), Type, Mode, Quantity, Mode == OrderMode.Limit ? Price : null);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Mode == OrderMode.Limit && Price is null)
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
            yield return new ValidationResult(localizer?["ValLimitPriceRequired"].Value ?? "ValLimitPriceRequired", [nameof(Price)]);
        }
    }
}

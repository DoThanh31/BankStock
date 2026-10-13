using System.ComponentModel.DataAnnotations;

namespace BankStock.Web.Validation;

/// <summary>
/// Khối lượng phải là bội số của <c>multiple</c> (lô 100 cổ phiếu). Thông báo lỗi đa ngôn ngữ và
/// validation phía client do <see cref="LotSizeAttributeAdapter"/> đảm nhận; Service kiểm tra lại lần nữa.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class LotSizeAttribute(int multiple) : ValidationAttribute
{
    public int Multiple { get; } = multiple;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is int quantity && quantity % Multiple != 0)
        {
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }
        return ValidationResult.Success;
    }

    public override string FormatErrorMessage(string name) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, Multiple);
}

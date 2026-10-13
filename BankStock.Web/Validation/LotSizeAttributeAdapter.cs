using System.Globalization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Validation;

/// <summary>
/// Adapter cho <see cref="LotSizeAttribute"/>: dịch thông báo lỗi qua resource ({0} = tên field, {1} = bội số)
/// và sinh data-val-lotsize cho jQuery Validation (wwwroot/js/lotsize-validation.js).
/// </summary>
public class LotSizeAttributeAdapter : AttributeAdapterBase<LotSizeAttribute>
{
    public LotSizeAttributeAdapter(LotSizeAttribute attribute, IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
    }

    public override void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-lotsize", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-lotsize-multiple", Attribute.Multiple.ToString(CultureInfo.InvariantCulture));
    }

    public override string GetErrorMessage(ModelValidationContextBase validationContext) =>
        GetErrorMessage(validationContext.ModelMetadata, validationContext.ModelMetadata.GetDisplayName(), Attribute.Multiple);
}

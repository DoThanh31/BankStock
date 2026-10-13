using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Validation;

/// <summary>
/// Đăng ký adapter cho attribute tùy biến; còn lại dùng provider mặc định của MVC.
/// [Required] không khai báo ErrorMessage (kể cả Required ngầm MVC thêm cho int/decimal/enum) được gán
/// khóa "ValRequired" để thông báo phía client cũng đa ngôn ngữ.
/// </summary>
public class AppValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    private const string RequiredMessageKey = "ValRequired";

    private readonly ValidationAttributeAdapterProvider _defaultProvider = new();

    public IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        if (attribute is LotSizeAttribute lotSize)
        {
            return new LotSizeAttributeAdapter(lotSize, stringLocalizer);
        }

        if (attribute is RequiredAttribute { ErrorMessage: null, ErrorMessageResourceName: null } required)
        {
            required.ErrorMessage = RequiredMessageKey;
        }

        return _defaultProvider.GetAttributeAdapter(attribute, stringLocalizer);
    }
}

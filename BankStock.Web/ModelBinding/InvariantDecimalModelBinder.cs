using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankStock.Web.ModelBinding;

/// <summary>
/// Bind decimal theo InvariantCulture trước (input type="number" luôn gửi dấu chấm thập phân),
/// sau đó mới thử culture hiện tại. Tránh lỗi vi-VN hiểu "15.2" thành 152.
/// </summary>
public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var raw = valueResult.FirstValue?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            // Để trống: null cho decimal?, còn decimal bắt buộc thì báo lỗi qua [Required]/binding.
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(raw ?? string.Empty));
            }
            return Task.CompletedTask;
        }

        if (decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            bindingContext.ModelMetadata.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(raw, bindingContext.ModelMetadata.GetDisplayName()));
        return Task.CompletedTask;
    }
}

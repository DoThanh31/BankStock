using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BankStock.Web.ModelBinding;

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type == typeof(decimal) ? new InvariantDecimalModelBinder() : null;
    }
}

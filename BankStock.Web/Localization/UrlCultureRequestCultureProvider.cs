using Microsoft.AspNetCore.Localization;

namespace BankStock.Web.Localization;

/// <summary>Provider ưu tiên cao nhất: culture từ tiền tố URL do <see cref="UrlCultureMiddleware"/> nhận diện.</summary>
public class UrlCultureRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var culture = UrlCulture.FromRequest(httpContext);
        return culture is null
            ? NullProviderCultureResult
            : Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
    }
}

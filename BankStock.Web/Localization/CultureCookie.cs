using BankStock.Web.Models;
using Microsoft.AspNetCore.Localization;

namespace BankStock.Web.Localization;

/// <summary>Ghi cookie ngôn ngữ (.AspNetCore.Culture) mà <see cref="CookieRequestCultureProvider"/> đọc.</summary>
public static class CultureCookie
{
    /// <summary>Ghi cookie cho culture được hỗ trợ; trả về tên chuẩn đã ghi hoặc null nếu culture không hỗ trợ.</summary>
    public static string? Write(HttpContext context, string? culture)
    {
        var canonical = UrlCulture.Canonical(culture);
        if (canonical is not null)
        {
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(canonical)),
                AppCookies.Preference(context.Request));
        }
        return canonical;
    }
}

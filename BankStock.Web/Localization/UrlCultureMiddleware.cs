namespace BankStock.Web.Localization;

/// <summary>
/// Tách tiền tố culture khỏi URL: <c>/en/Stocks</c> → PathBase "/en", Path "/Stocks".
/// Nhờ vậy routing, static files, Areas và API giữ nguyên route; mọi URL sinh ra bởi
/// <c>Url.Action</c>, tag helper hay <c>~/</c> tự mang tiền tố "/en" nên người dùng ở lại đúng ngôn ngữ.
/// Phải đặt đầu pipeline (trước localization, static files, routing, authentication).
/// </summary>
public class UrlCultureMiddleware
{
    private readonly RequestDelegate _next;

    public UrlCultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value ?? string.Empty;
        var segmentEnd = path.IndexOf('/', 1);
        var segment = path.Length > 1 ? (segmentEnd < 0 ? path[1..] : path[1..segmentEnd]) : null;

        if (!UrlCulture.TryGetCulture(segment, out var culture))
        {
            await _next(context);
            return;
        }

        var originalPathBase = request.PathBase;
        var originalPath = request.Path;
        context.Items[UrlCulture.ItemKey] = culture;
        // Chuẩn hóa chữ thường (/EN/... → PathBase "/en") để link sinh ra thống nhất.
        request.PathBase = originalPathBase.Add(new PathString("/" + segment!.ToLowerInvariant()));
        request.Path = segmentEnd < 0 ? new PathString("/") : new PathString(path[segmentEnd..]);

        try
        {
            await _next(context);
        }
        finally
        {
            request.PathBase = originalPathBase;
            request.Path = originalPath;
        }
    }
}

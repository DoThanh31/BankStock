namespace BankStock.Web.Security;

/// <summary>
/// Gắn header bảo mật cho mọi response (trang, AJAX, API, file tĩnh, trang lỗi):
/// <list type="bullet">
/// <item><c>Content-Security-Policy</c>: script chỉ từ chính site + cdn.jsdelivr.net (kèm SRI), không có script inline / eval
/// (AngularJS chạy chế độ <c>ng-csp</c>), không cho nhúng trang vào iframe, form chỉ gửi về chính site.</item>
/// <item><c>X-Content-Type-Options</c>, <c>X-Frame-Options</c>, <c>Referrer-Policy</c>, <c>Permissions-Policy</c>,
/// <c>Cross-Origin-Opener-Policy</c>.</item>
/// </list>
/// Header được thêm ở <see cref="HttpResponse.OnStarting(Func{Task})"/> vì exception handler xóa header đã đặt trước đó.
/// </summary>
public class SecurityHeadersMiddleware
{
    private const string CdnOrigin = "https://cdn.jsdelivr.net";

    // Font Inter / JetBrains Mono của theme (theme.css @import): CSS từ googleapis, file font từ gstatic.
    private const string GoogleFontsCssOrigin = "https://fonts.googleapis.com";
    private const string GoogleFontsFileOrigin = "https://fonts.gstatic.com";

    private readonly RequestDelegate _next;
    private readonly string _contentSecurityPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _contentSecurityPolicy = BuildContentSecurityPolicy(upgradeInsecureRequests: !environment.IsDevelopment());
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Swagger UI (chỉ bật ở Development) dùng script/style inline của Swashbuckle → không áp CSP cho riêng đường dẫn này.
        bool applyCsp = !context.Request.Path.StartsWithSegments("/swagger");

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            if (applyCsp)
            {
                headers.ContentSecurityPolicy = _contentSecurityPolicy;
            }
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static string BuildContentSecurityPolicy(bool upgradeInsecureRequests)
    {
        var directives = new List<string>
        {
            "default-src 'self'",
            $"script-src 'self' {CdnOrigin}",
            // 'unsafe-inline' chỉ cho style: SweetAlert2 chèn <style>, một số thuộc tính style="" trong view. Script vẫn bị chặn inline.
            $"style-src 'self' 'unsafe-inline' {CdnOrigin} {GoogleFontsCssOrigin}",
            $"font-src 'self' {CdnOrigin} {GoogleFontsFileOrigin} data:",
            // Logo ngân hàng do Admin nhập có thể là URL https bên ngoài.
            "img-src 'self' data: https:",
            "connect-src 'self'",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'"
        };
        if (upgradeInsecureRequests)
        {
            directives.Add("upgrade-insecure-requests");
        }
        return string.Join("; ", directives);
    }
}

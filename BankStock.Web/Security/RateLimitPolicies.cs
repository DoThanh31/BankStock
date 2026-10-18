using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Security;

/// <summary>
/// Giới hạn tần suất request (ASP.NET Core Rate Limiting, fixed window):
/// <list type="bullet">
/// <item><see cref="Auth"/>: POST đăng nhập / đăng ký / quên và đặt lại mật khẩu, theo IP — chống dò mật khẩu hàng loạt
/// (bổ sung cho lockout 5 lần sai của từng tài khoản).</item>
/// <item><see cref="Api"/>: toàn bộ <c>/api/v1</c>, theo UserId (đã đăng nhập) hoặc IP.</item>
/// </list>
/// Vượt giới hạn → 429 + <c>Retry-After</c>; API trả ProblemDetails, trang MVC hiển thị trang lỗi 429.
/// Ngưỡng cấu hình ở mục <c>RateLimiting</c> trong appsettings.json.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Api = "api";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection("RateLimiting:Auth").Get<RateLimitOptions>() ?? new RateLimitOptions { PermitLimit = 10 };
        var api = configuration.GetSection("RateLimiting:Api").Get<RateLimitOptions>() ?? new RateLimitOptions { PermitLimit = 120 };

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Auth, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => auth.ToLimiterOptions()));

            options.AddPolicy(Api, context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var key = userId is null ? "ip:" + ClientIp(context) : "user:" + userId;
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => api.ToLimiterOptions());
            });

            options.OnRejected = OnRejectedAsync;
        });
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitPolicies));
        logger.LogWarning("Rate limit exceeded: {Method} {Path} from {IpAddress}.", http.Request.Method, http.Request.Path, ClientIp(http));

        // Trang MVC: để trống body, UseStatusCodePagesWithReExecute hiển thị trang lỗi 429 thân thiện.
        if (!http.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        var localizer = http.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = localizer["Error429Title"].Value,
                Detail = localizer["Error429Message"].Value
            }
        });
    }

    private static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>Ngưỡng của một policy: tối đa <see cref="PermitLimit"/> request mỗi <see cref="WindowSeconds"/> giây.</summary>
    public sealed class RateLimitOptions
    {
        public int PermitLimit { get; set; }
        public int WindowSeconds { get; set; } = 60;

        public FixedWindowRateLimiterOptions ToLimiterOptions() => new()
        {
            PermitLimit = Math.Max(1, PermitLimit),
            Window = TimeSpan.FromSeconds(Math.Max(1, WindowSeconds)),
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }
}

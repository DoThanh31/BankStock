using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankStock.Web.Filters;

/// <summary>
/// Filter toàn cục ghi vết (audit trail) mỗi action MVC / API qua <see cref="ILogger"/>: ai (UserId, IP),
/// làm gì (method, area/controller/action, id), kết quả (status code) và thời gian xử lý.
/// Thao tác ghi (POST/PUT/DELETE) ở mức Information, đọc ở mức Debug, lỗi / bị từ chối (4xx, 5xx) ở mức Warning.
/// Không ghi body / tham số form (có thể chứa mật khẩu). Sự kiện nghiệp vụ (đặt lệnh, CRUD Admin, đăng nhập)
/// vẫn do Service ghi vào bảng AuditLog trong cùng giao dịch dữ liệu.
/// </summary>
/// <remarks>
/// Dùng resource filter để bao cả model binding lẫn bước thực thi kết quả, nên status code ghi lại là giá trị cuối cùng.
/// </remarks>
public sealed class LogActionFilter : IAsyncResourceFilter
{
    private readonly ILogger<LogActionFilter> _logger;

    public LogActionFilter(ILogger<LogActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var executed = await next();
        stopwatch.Stop();

        var http = context.HttpContext;
        bool isWrite = !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method);
        bool failed = executed.Exception is not null && !executed.ExceptionHandled;
        int statusCode = failed ? StatusCodes.Status500InternalServerError : http.Response.StatusCode;

        var level = statusCode switch
        {
            // Chi tiết exception (5xx) do GlobalExceptionFilter ghi ở mức Error; ở đây chỉ cần vết request.
            >= 400 => LogLevel.Warning,
            _ when isWrite => LogLevel.Information,
            _ => LogLevel.Debug
        };
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        var route = context.RouteData.Values;
        _logger.Log(level,
            "Audit {Method} {Area}/{Controller}/{Action} id={RouteId} → {StatusCode} in {ElapsedMs} ms (user={UserId}, ip={IpAddress}, trace={TraceId})",
            http.Request.Method,
            route["area"] ?? "-",
            route["controller"],
            route["action"],
            route["id"] ?? route["code"] ?? "-",
            statusCode,
            stopwatch.ElapsedMilliseconds,
            http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            http.Connection.RemoteIpAddress?.ToString(),
            Activity.Current?.Id ?? http.TraceIdentifier);
    }
}

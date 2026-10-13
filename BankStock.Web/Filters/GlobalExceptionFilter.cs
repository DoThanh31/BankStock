using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Filters;

/// <summary>
/// Filter toàn cục bắt exception chưa xử lý trong action MVC / API:
/// <list type="bullet">
/// <item>API (<c>[ApiController]</c>) → <see cref="ProblemDetails"/> (500, hoặc 409 khi xung đột dữ liệu) kèm traceId.</item>
/// <item>Request AJAX của trang MVC → JSON <c>{ success: false, message }</c> để SweetAlert2 hiển thị.</item>
/// <item>Trang MVC thường → để exception đi tiếp tới <c>UseExceptionHandler("/Home/Error")</c>
/// (Development: trang lỗi chi tiết cho lập trình viên).</item>
/// </list>
/// Không bao giờ trả stack trace / thông điệp exception cho client.
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<SharedResource> localizer,
        ILogger<GlobalExceptionFilter> logger)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _localizer = localizer;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        var http = context.HttpContext;
        var exception = context.Exception;

        // Client đóng kết nối giữa chừng: không phải lỗi hệ thống, không cần trả nội dung.
        if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client.", http.Request.Method, http.Request.Path);
            context.Result = new EmptyResult();
            context.ExceptionHandled = true;
            return;
        }

        var (statusCode, messageKey) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "ConcurrencyConflict"),
            _ => (StatusCodes.Status500InternalServerError, "ErrorDefaultMessage")
        };
        var traceId = Activity.Current?.Id ?? http.TraceIdentifier;

        _logger.Log(statusCode >= 500 ? LogLevel.Error : LogLevel.Warning, exception,
            "Unhandled exception in {Action} ({Method} {Path}), trace {TraceId}.",
            context.ActionDescriptor.DisplayName, http.Request.Method, http.Request.Path, traceId);

        var message = _localizer[messageKey].Value;
        if (IsApiAction(context))
        {
            var problem = _problemDetailsFactory.CreateProblemDetails(http, statusCode, title: _localizer["ErrorDefaultTitle"].Value, detail: message);
            context.Result = new ObjectResult(problem) { StatusCode = statusCode, ContentTypes = { "application/problem+json" } };
            context.ExceptionHandled = true;
        }
        else if (IsAjaxRequest(http.Request))
        {
            context.Result = new JsonResult(new { success = false, message, traceId }) { StatusCode = statusCode };
            context.ExceptionHandled = true;
        }
    }

    private static bool IsApiAction(ExceptionContext context) =>
        context.ActionDescriptor.EndpointMetadata.OfType<IApiBehaviorMetadata>().Any();

    private static bool IsAjaxRequest(HttpRequest request) =>
        string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}

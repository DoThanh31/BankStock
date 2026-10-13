using System.Security.Claims;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>
/// Controller nền của REST API (/api/v1): JSON, lỗi trả <see cref="ProblemDetails"/>, CORS theo whitelist.
/// Xác thực bằng cookie nên mọi thao tác ghi (POST/PUT/DELETE) bắt buộc header RequestVerificationToken (chống CSRF).
/// </summary>
[ApiController]
[AutoValidateAntiforgeryToken]
[EnableCors(ApiDefaults.CorsPolicy)]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ApiControllerBase(IStringLocalizer<SharedResource> localizer)
    {
        Localizer = localizer;
    }

    protected IStringLocalizer<SharedResource> Localizer { get; }

    /// <summary>UserId lấy từ cookie đăng nhập; không bao giờ nhận từ request (chống IDOR).</summary>
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>
    /// Đổi lỗi của Service sang HTTP: lỗi gắn với field → 400 <see cref="ValidationProblemDetails"/>;
    /// NotFound → 404, Conflict → 409, còn lại 400 (<see cref="ProblemDetails"/>).
    /// </summary>
    protected ActionResult ProblemFrom(ServiceResult result)
    {
        var message = Localizer[result.ErrorKey ?? "AdminOperationFailed", result.ErrorArgs].Value;
        if (result.ErrorType == ServiceErrorType.Validation && result.Field is not null)
        {
            ModelState.AddModelError(result.Field, message);
            return ValidationProblem(ModelState);
        }

        var status = result.ErrorType switch
        {
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(statusCode: status, detail: message);
    }

    protected ActionResult NotFoundProblem(string detail) =>
        Problem(statusCode: StatusCodes.Status404NotFound, detail: detail);
}

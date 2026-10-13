using System.Security.Claims;
using BankStock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers;

/// <summary>
/// Helper dùng chung cho controller MVC có thao tác AJAX (khu Admin, Investor):
/// partial form 400 khi lỗi validation, JSON { success, message } khi thành công/thất bại.
/// </summary>
public abstract class AppControllerBase : Controller
{
    protected AppControllerBase(IStringLocalizer<SharedResource> localizer)
    {
        Localizer = localizer;
    }

    protected IStringLocalizer<SharedResource> Localizer { get; }

    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    protected bool IsAjaxRequest =>
        string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    /// <summary>Trang danh sách: AJAX trả partial kết quả, còn lại trả cả trang.</summary>
    protected IActionResult ListView(object model, string partialName = "_List") =>
        IsAjaxRequest ? PartialView(partialName, model) : View(model);

    /// <summary>Form lỗi validation: trả lại partial với mã 400 để JS hiển thị lại.</summary>
    protected IActionResult InvalidForm(string partialName, object model)
    {
        var result = PartialView(partialName, model);
        result.StatusCode = StatusCodes.Status400BadRequest;
        return result;
    }

    protected IActionResult JsonSuccess(string messageKey, params object[] args) =>
        Json(new { success = true, message = Localizer[messageKey, args].Value });

    protected IActionResult JsonError(int statusCode, string message)
    {
        Response.StatusCode = statusCode;
        return Json(new { success = false, message });
    }

    /// <summary>
    /// Kết quả thao tác form: thành công → JSON; lỗi gắn với field → partial form 400; NotFound → JSON lỗi 404.
    /// </summary>
    protected IActionResult FormResult(ServiceResult result, string partialName, object model, string successKey, params object[] successArgs)
    {
        if (result.Succeeded)
        {
            return JsonSuccess(successKey, successArgs);
        }

        if (result.ErrorType == ServiceErrorType.NotFound)
        {
            return JsonError(StatusCodes.Status404NotFound, ErrorMessage(result));
        }

        ModelState.AddModelError(result.Field ?? string.Empty, ErrorMessage(result));
        return InvalidForm(partialName, model);
    }

    /// <summary>Kết quả thao tác không có form (xóa, khóa, hủy lệnh...): luôn trả JSON với status code tương ứng.</summary>
    protected IActionResult ActionResultJson(ServiceResult result, string successKey, params object[] successArgs)
    {
        if (result.Succeeded)
        {
            return JsonSuccess(successKey, successArgs);
        }

        return JsonError(StatusCodeOf(result), ErrorMessage(result));
    }

    protected static int StatusCodeOf(ServiceResult result) => result.ErrorType switch
    {
        ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
        ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    protected string ErrorMessage(ServiceResult result) =>
        Localizer[result.ErrorKey ?? "AdminOperationFailed", result.ErrorArgs].Value;

    /// <summary>Lỗi ModelState đầu tiên (dùng cho form AJAX trả JSON thay vì partial).</summary>
    protected string FirstModelError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m))
        ?? Localizer["AdminOperationFailed"].Value;
}

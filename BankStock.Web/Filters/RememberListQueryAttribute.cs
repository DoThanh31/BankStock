using BankStock.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankStock.Web.Filters;

/// <summary>
/// Gắn lên action danh sách (GET) để nhớ bộ lọc trong Session:
/// <list type="bullet">
/// <item>Có query string → lưu lại.</item>
/// <item>Không có query string (vào lại từ menu) → chuyển hướng về URL kèm bộ lọc đã lưu.</item>
/// <item><c>?reset=true</c> (nút "Xóa lọc") → xóa bộ lọc đã lưu.</item>
/// </list>
/// Request AJAX không bị chuyển hướng (JS tự quản lý URL).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RememberListQueryAttribute : ActionFilterAttribute
{
    public const string ResetParameter = "reset";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method))
        {
            return;
        }

        var store = context.HttpContext.RequestServices.GetRequiredService<IListQueryStateService>();
        var listKey = ListKey(context.RouteData);
        bool isAjax = string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var path = string.Concat(request.PathBase, request.Path);

        if (request.Query.ContainsKey(ResetParameter))
        {
            store.Clear(listKey);
            if (!isAjax)
            {
                context.Result = new LocalRedirectResult(path);
            }
            return;
        }

        if (request.QueryString.HasValue)
        {
            store.Save(listKey, request.QueryString.Value!);
            return;
        }

        if (!isAjax && store.Get(listKey) is { } saved)
        {
            context.Result = new LocalRedirectResult(path + saved);
        }
    }

    private static string ListKey(RouteData routeData) =>
        $"{routeData.Values["area"]}/{routeData.Values["controller"]}/{routeData.Values["action"]}";
}

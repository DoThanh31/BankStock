using System.Globalization;
using BankStock.Web.Models;
using BankStock.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankStock.Web.Filters;

/// <summary>
/// Filter toàn cục cho action có tham số <see cref="IPageSizeQuery"/>: URL có <c>pageSize</c> hợp lệ → ghi cookie;
/// URL không có → lấy page size từ cookie (nếu trang đó có kích thước này trong ô chọn).
/// </summary>
public sealed class PageSizeCookieFilter : IActionFilter
{
    private const string QueryKey = "pageSize";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var query = context.ActionArguments.Values.OfType<IPageSizeQuery>().FirstOrDefault();
        if (query is null)
        {
            return;
        }

        var request = context.HttpContext.Request;
        if (request.Query.ContainsKey(QueryKey))
        {
            if (query.IsSelectablePageSize(query.PageSize))
            {
                var value = query.PageSize.ToString(CultureInfo.InvariantCulture);
                if (request.Cookies[AppCookies.PageSize] != value)
                {
                    context.HttpContext.Response.Cookies.Append(AppCookies.PageSize, value, AppCookies.Preference(request));
                }
            }
            return;
        }

        if (int.TryParse(request.Cookies[AppCookies.PageSize], NumberStyles.None, CultureInfo.InvariantCulture, out var saved)
            && query.IsSelectablePageSize(saved))
        {
            query.PageSize = saved;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

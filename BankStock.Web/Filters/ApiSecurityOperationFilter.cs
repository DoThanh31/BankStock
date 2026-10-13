using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BankStock.Web.Filters;

/// <summary>
/// Swagger: đánh dấu ổ khóa cho endpoint cần đăng nhập (cookie) và endpoint ghi cần header antiforgery,
/// để "Try it out" gửi đúng thông tin xác thực.
/// </summary>
public class ApiSecurityOperationFilter : IOperationFilter
{
    public const string CookieScheme = "cookieAuth";
    public const string AntiforgeryScheme = "antiforgery";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        bool requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        bool isWrite = !HttpMethods.IsGet(context.ApiDescription.HttpMethod ?? HttpMethods.Get);
        if (!requiresAuth && !isWrite)
        {
            return;
        }

        var requirement = new OpenApiSecurityRequirement();
        if (requiresAuth)
        {
            requirement[new OpenApiSecuritySchemeReference(CookieScheme, context.Document)] = [];
        }
        if (isWrite)
        {
            requirement[new OpenApiSecuritySchemeReference(AntiforgeryScheme, context.Document)] = [];
        }

        operation.Security ??= [];
        operation.Security.Add(requirement);
    }
}

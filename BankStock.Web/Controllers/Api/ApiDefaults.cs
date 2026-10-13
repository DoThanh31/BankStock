namespace BankStock.Web.Controllers.Api;

/// <summary>Hằng số dùng chung của Web API: tiền tố route theo phiên bản, tên tài liệu Swagger, policy CORS.</summary>
public static class ApiDefaults
{
    public const string Version = "v1";
    public const string RoutePrefix = "api/" + Version;
    public const string CorsPolicy = "ApiCors";
}

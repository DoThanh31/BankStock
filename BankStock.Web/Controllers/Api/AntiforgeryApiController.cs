using BankStock.Web.Dtos;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers.Api;

/// <summary>
/// Cấp token chống CSRF cho client gọi API ngoài Razor (Swagger UI, SPA cùng origin). Token gắn với người dùng
/// hiện tại nên phải lấy lại sau khi đăng nhập / đăng xuất.
/// </summary>
[AllowAnonymous]
[Route(ApiDefaults.RoutePrefix + "/antiforgery")]
public class AntiforgeryApiController : ApiControllerBase
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryApiController(IAntiforgery antiforgery, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _antiforgery = antiforgery;
    }

    /// <summary>Đặt cookie antiforgery và trả token để gửi trong header của các request ghi.</summary>
    [HttpGet("token")]
    [ProducesResponseType(typeof(AntiforgeryTokenDto), StatusCodes.Status200OK)]
    public ActionResult<AntiforgeryTokenDto> GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new AntiforgeryTokenDto(tokens.HeaderName!, tokens.RequestToken!));
    }
}

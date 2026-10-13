using BankStock.Web.Controllers;
using BankStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

/// <summary>
/// Controller nền của khu Admin: chỉ role Admin, kiểm tra antiforgery cho mọi POST.
/// Helper trả kết quả AJAX nằm ở <see cref="AppControllerBase"/>.
/// </summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
public abstract class AdminControllerBase : AppControllerBase
{
    protected AdminControllerBase(IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
    }
}

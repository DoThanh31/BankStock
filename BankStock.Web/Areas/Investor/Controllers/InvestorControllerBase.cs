using BankStock.Web.Controllers;
using BankStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Investor.Controllers;

/// <summary>
/// Controller nền khu Investor: chỉ role Investor, antiforgery cho mọi POST.
/// Dữ liệu luôn lấy theo <see cref="AppControllerBase.CurrentUserId"/> — không nhận UserId từ request (chống IDOR).
/// </summary>
[Area("Investor")]
[Authorize(Roles = AppRoles.Investor)]
[AutoValidateAntiforgeryToken]
public abstract class InvestorControllerBase : AppControllerBase
{
    protected InvestorControllerBase(IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
    }
}

using BankStock.Web.Filters;
using BankStock.Web.Services;
using BankStock.Web.ViewModels;
using BankStock.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Areas.Admin.Controllers;

public class UsersController : AdminControllerBase
{
    private const string RolesPartial = "_RolesForm";

    private readonly IUserAdminService _userService;

    public UsersController(IUserAdminService userService, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _userService = userService;
    }

    [HttpGet]
    [RememberListQuery]
    public async Task<IActionResult> Index([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        var items = await _userService.GetPagedAsync(query, cancellationToken);
        ViewData["CurrentUserId"] = CurrentUserId;
        return ListView(new PagedListViewModel<UserAdminListItem, UserListQuery>(query, items));
    }

    [HttpPost]
    public async Task<IActionResult> Lock(string id, CancellationToken cancellationToken)
    {
        var result = await _userService.SetActiveAsync(id, isActive: false, CurrentUserId, cancellationToken);
        return ActionResultJson(result, "AdminUserLocked");
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(string id, CancellationToken cancellationToken)
    {
        var result = await _userService.SetActiveAsync(id, isActive: true, CurrentUserId, cancellationToken);
        return ActionResultJson(result, "AdminUserUnlocked");
    }

    [HttpGet]
    public async Task<IActionResult> Roles(string id, CancellationToken cancellationToken)
    {
        var model = await _userService.GetRolesAsync(id, cancellationToken);
        return model is null ? NotFound() : PartialView(RolesPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Roles(string id, UserRolesViewModel model, CancellationToken cancellationToken)
    {
        model.UserId = id;
        if (!ModelState.IsValid)
        {
            return InvalidForm(RolesPartial, model);
        }

        var result = await _userService.UpdateRolesAsync(model, CurrentUserId, cancellationToken);
        return FormResult(result, RolesPartial, model, "AdminRolesUpdated", model.Email);
    }
}

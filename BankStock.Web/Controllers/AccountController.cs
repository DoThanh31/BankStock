using System.Globalization;
using System.Security.Claims;
using System.Text;
using BankStock.Web.Dtos;
using BankStock.Web.Models;
using BankStock.Web.Services;
using BankStock.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace BankStock.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private const string StatusMessageKey = "StatusMessage";
    private const string DevResetLinkKey = "DevResetLink";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccountService _accountService;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccountService accountService,
        IAuditLogService auditLog,
        IEmailSender emailSender,
        IStringLocalizer<SharedResource> localizer,
        IWebHostEnvironment environment,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountService = accountService;
        _auditLog = auditLog;
        _emailSender = emailSender;
        _localizer = localizer;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;

        // Cookie "ghi nhớ": điền sẵn email lần đăng nhập trước và giữ dấu chọn "Ghi nhớ đăng nhập".
        var rememberedEmail = Request.Cookies[AppCookies.RememberEmail];
        return View(new LoginViewModel
        {
            Email = rememberedEmail ?? string.Empty,
            RememberMe = rememberedEmail is not null
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            await _auditLog.LogAsync("LoginFailed", null, nameof(ApplicationUser), null, $"Unknown email: {model.Email}", cancellationToken);
            ModelState.AddModelError(string.Empty, _localizer["LoginInvalid"]);
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditLog.LogAsync("LoginBlocked", user.Id, nameof(ApplicationUser), user.Id, "Account disabled", cancellationToken);
            ModelState.AddModelError(string.Empty, _localizer["LoginDisabled"]);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _auditLog.LogAsync("Login", user.Id, nameof(ApplicationUser), user.Id, null, cancellationToken);
            StartFreshSession();
            RememberLoginEmail(model);

            if (string.IsNullOrEmpty(returnUrl) && await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            await _auditLog.LogAsync("LoginLockedOut", user.Id, nameof(ApplicationUser), user.Id, null, cancellationToken);
            ModelState.AddModelError(string.Empty, _localizer["LoginLockedOut"]);
            return View(model);
        }

        await _auditLog.LogAsync("LoginFailed", user.Id, nameof(ApplicationUser), user.Id, "Wrong password", cancellationToken);
        ModelState.AddModelError(string.Empty, _localizer["LoginInvalid"]);
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new RegisterInvestorCommand(model.FullName, model.Email, model.Password, CultureInfo.CurrentUICulture.Name);
        var (result, user) = await _accountService.RegisterInvestorAsync(command, cancellationToken);
        if (!result.Succeeded || user is null)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _auditLog.LogAsync("Register", user.Id, nameof(ApplicationUser), user.Id, "Investor + wallet initial grant", cancellationToken);
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData[StatusMessageKey] = _localizer["RegisterSuccess"].Value;
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _signInManager.SignOutAsync();
        StartFreshSession();
        await _auditLog.LogAsync("Logout", userId, nameof(ApplicationUser), userId, null, cancellationToken);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Luôn chuyển sang trang xác nhận dù email có tồn tại hay không để tránh dò tài khoản.
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, code }, Request.Scheme);

            await _emailSender.SendAsync(user.Email!, _localizer["ResetPasswordEmailSubject"], $"{_localizer["ResetPasswordEmailBody"]} {callbackUrl}", cancellationToken);
            await _auditLog.LogAsync("ForgotPassword", user.Id, nameof(ApplicationUser), user.Id, null, cancellationToken);

            // Môi trường dev không có SMTP: hiển thị link ngay trên trang để kiểm thử.
            if (_environment.IsDevelopment())
            {
                TempData[DevResetLinkKey] = callbackUrl;
            }
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        ViewData[DevResetLinkKey] = TempData[DevResetLinkKey] as string;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? code = null, string? email = null)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest();
        }

        return View(new ResetPasswordViewModel { Code = code, Email = email ?? string.Empty });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Malformed password reset code for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, _localizer["ResetPasswordInvalidToken"]);
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _auditLog.LogAsync("ResetPassword", user.Id, nameof(ApplicationUser), user.Id, null, cancellationToken);
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>Xóa Session (giỏ lệnh nháp, bộ lọc...) khi đổi người dùng trên cùng trình duyệt.</summary>
    private void StartFreshSession() => HttpContext.Session.Clear();

    /// <summary>Có chọn "Ghi nhớ đăng nhập" → lưu email vào cookie để lần sau điền sẵn; bỏ chọn → xóa cookie.</summary>
    private void RememberLoginEmail(LoginViewModel model)
    {
        if (model.RememberMe)
        {
            var options = AppCookies.Preference(Request);
            options.Expires = DateTimeOffset.UtcNow.Add(AppCookies.RememberMeLifetime);
            Response.Cookies.Append(AppCookies.RememberEmail, model.Email, options);
        }
        else
        {
            Response.Cookies.Delete(AppCookies.RememberEmail, new CookieOptions { Path = AppCookies.RootPath });
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>Đổi các lỗi Identity thường gặp sang thông báo đa ngôn ngữ.</summary>
    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var message = error.Code switch
            {
                nameof(IdentityErrorDescriber.DuplicateUserName) or nameof(IdentityErrorDescriber.DuplicateEmail) => _localizer["EmailTaken"].Value,
                nameof(IdentityErrorDescriber.InvalidToken) => _localizer["ResetPasswordInvalidToken"].Value,
                _ when error.Code.StartsWith("Password", StringComparison.Ordinal) => _localizer["ValPasswordComplexity"].Value,
                _ => error.Description
            };

            if (!ModelState[string.Empty]?.Errors.Any(e => e.ErrorMessage == message) ?? true)
            {
                ModelState.AddModelError(string.Empty, message);
            }
        }
    }
}

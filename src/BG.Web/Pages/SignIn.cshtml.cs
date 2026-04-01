using System.Security.Claims;
using BG.Application.Contracts.Services;
using BG.Web.Localization;
using BG.Web.Security;
using BG.Web.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages;

public sealed class SignInModel : PageModel
{
    private readonly ILocalAuthenticationService _localAuthenticationService;
    private readonly ILoginAttemptLockoutService _loginAttemptLockoutService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SignInModel(
        ILocalAuthenticationService localAuthenticationService,
        ILoginAttemptLockoutService loginAttemptLockoutService,
        IStringLocalizer<SharedResource> localizer)
    {
        _localAuthenticationService = localAuthenticationService;
        _loginAttemptLockoutService = loginAttemptLockoutService;
        _localizer = localizer;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ShellMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(NormalizeReturnUrl(ReturnUrl));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
        var lockoutDecision = _loginAttemptLockoutService.GetDecision(Username, remoteIpAddress);
        if (lockoutDecision.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, _localizer["identity.login_temporarily_locked"]);
            return Page();
        }

        var result = await _localAuthenticationService.AuthenticateAsync(
            new BG.Application.Models.Identity.AuthenticateLocalUserCommand(Username, Password),
            cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            var updatedDecision = _loginAttemptLockoutService.RegisterFailure(Username, remoteIpAddress);
            var errorCode = updatedDecision.IsLockedOut
                ? "identity.login_temporarily_locked"
                : result.ErrorCode ?? "identity.invalid_credentials";

            ModelState.AddModelError(string.Empty, _localizer[errorCode]);
            return Page();
        }

        _loginAttemptLockoutService.Reset(Username, remoteIpAddress);

        var profile = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
            new(ClaimTypes.Name, profile.DisplayName),
            new("preferred_username", profile.Username)
        };

        claims.AddRange(profile.RoleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));
        claims.AddRange(profile.PermissionKeys.Select(permissionKey => new Claim("bg.permission", permissionKey)));

        var identity = new ClaimsIdentity(claims, WorkspaceShellDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            WorkspaceShellDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

        return LocalRedirect(NormalizeReturnUrl(ReturnUrl));
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync(WorkspaceShellDefaults.AuthenticationScheme);
        return LocalRedirect("/SignIn");
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            return "/";
        }

        return returnUrl;
    }
}

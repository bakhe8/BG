using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Localization;
using BG.Web.Pages;
using BG.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.UnitTests.Web;

public sealed class SignInPageTests
{
    [Fact]
    public async Task OnPostAsync_returns_page_without_authenticating_when_identity_is_locked()
    {
        var authenticationService = new StubLocalAuthenticationService();
        var model = new SignInModel(
            authenticationService,
            new StubLoginAttemptLockoutService(isLockedOut: true),
            new StubStringLocalizer())
        {
            Username = "request.user",
            Password = "wrong-password"
        };

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(authenticationService.WasCalled);
        Assert.Contains(
            model.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage == "Temporarily locked");
    }

    private sealed class StubLocalAuthenticationService : ILocalAuthenticationService
    {
        public bool WasCalled { get; private set; }

        public Task<OperationResult<UserAccessProfileDto>> AuthenticateAsync(
            AuthenticateLocalUserCommand command,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(OperationResult<UserAccessProfileDto>.Failure("identity.invalid_credentials"));
        }
    }

    private sealed class StubLoginAttemptLockoutService : ILoginAttemptLockoutService
    {
        private readonly bool _isLockedOut;

        public StubLoginAttemptLockoutService(bool isLockedOut)
        {
            _isLockedOut = isLockedOut;
        }

        public LoginLockoutDecision GetDecision(string? username, System.Net.IPAddress? remoteIpAddress)
        {
            return new LoginLockoutDecision(_isLockedOut, _isLockedOut ? 5 : 0, _isLockedOut ? DateTimeOffset.UtcNow.AddMinutes(5) : null);
        }

        public LoginLockoutDecision RegisterFailure(string? username, System.Net.IPAddress? remoteIpAddress)
        {
            return new LoginLockoutDecision(true, 5, DateTimeOffset.UtcNow.AddMinutes(5));
        }

        public void Reset(string? username, System.Net.IPAddress? remoteIpAddress)
        {
        }
    }

    private sealed class StubStringLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name]
            => new(name, name == "identity.login_temporarily_locked" ? "Temporarily locked" : name, false);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Array.Empty<LocalizedString>();
        }

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
        {
            return this;
        }
    }
}

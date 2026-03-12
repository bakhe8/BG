using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Controllers;
using BG.Web.Contracts.Identity;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BG.UnitTests.Web;

public sealed class IdentityAdministrationControllerTests
{
    [Fact]
    public async Task CreateUser_returns_bad_request_with_error_code_when_service_fails()
    {
        var controller = new IdentityAdministrationController(
            new StubIdentityAdministrationService(
                createUserResult: OperationResult<UserSummaryDto>.Failure("identity.duplicate_username")),
            new StubStringLocalizer());

        var result = await controller.CreateUser(
            new CreateUserRequest
            {
                Username = "admin",
                DisplayName = "Admin",
                InitialPassword = "initial-password"
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);

        Assert.Equal("Identity operation failed", problemDetails.Title);
        Assert.Equal("This username already exists.", problemDetails.Detail);
        Assert.Equal("identity.duplicate_username", problemDetails.Extensions["errorCode"]);
    }

    [Fact]
    public async Task GetPermissions_returns_ok_response()
    {
        var controller = new IdentityAdministrationController(
            new StubIdentityAdministrationService(
                permissions:
                [
                    new PermissionDto("users.view", "Administration")
                ]),
            new StubStringLocalizer());

        var result = await controller.GetPermissions(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var permissions = Assert.IsAssignableFrom<IReadOnlyList<PermissionDto>>(ok.Value);
        Assert.Single(permissions);
    }

    [Fact]
    public void Identity_administration_actions_are_protected_by_expected_policies()
    {
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.GetUsers), PermissionPolicyNames.UsersRead);
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.CreateUser), PermissionPolicyNames.UsersManage);
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.SetUserPassword), PermissionPolicyNames.UsersManage);
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.GetRoles), PermissionPolicyNames.RolesRead);
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.CreateRole), PermissionPolicyNames.RolesManage);
        AssertAuthorizePolicy(nameof(IdentityAdministrationController.GetPermissions), PermissionPolicyNames.RolesRead);
    }

    private static void AssertAuthorizePolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(IdentityAdministrationController).GetMethod(methodName);
        Assert.NotNull(method);

        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        Assert.Equal(expectedPolicy, attribute.Policy);
    }

    private sealed class StubIdentityAdministrationService : IIdentityAdministrationService
    {
        private readonly OperationResult<UserSummaryDto> _createUserResult;
        private readonly OperationResult<RoleSummaryDto> _createRoleResult;
        private readonly IReadOnlyList<UserSummaryDto> _users;
        private readonly IReadOnlyList<RoleSummaryDto> _roles;
        private readonly IReadOnlyList<PermissionDto> _permissions;

        public StubIdentityAdministrationService(
            OperationResult<UserSummaryDto>? createUserResult = null,
            OperationResult<RoleSummaryDto>? createRoleResult = null,
            IReadOnlyList<UserSummaryDto>? users = null,
            IReadOnlyList<RoleSummaryDto>? roles = null,
            IReadOnlyList<PermissionDto>? permissions = null)
        {
            _createUserResult = createUserResult ?? OperationResult<UserSummaryDto>.Success(
                new UserSummaryDto(
                    Guid.NewGuid(),
                    "user",
                    "User",
                    null,
                    null,
                    "Local",
                    true,
                    true,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    Array.Empty<string>()));
            _createRoleResult = createRoleResult ?? OperationResult<RoleSummaryDto>.Success(
                new RoleSummaryDto(Guid.NewGuid(), "Role", null, Array.Empty<string>()));
            _users = users ?? Array.Empty<UserSummaryDto>();
            _roles = roles ?? Array.Empty<RoleSummaryDto>();
            _permissions = permissions ?? Array.Empty<PermissionDto>();
        }

        public Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users);
        }

        public Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_roles);
        }

        public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_permissions);
        }

        public Task<OperationResult<UserSummaryDto>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_createUserResult);
        }

        public Task<OperationResult<UserSummaryDto>> SetUserPasswordAsync(SetLocalUserPasswordCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_createUserResult);
        }

        public Task<OperationResult<RoleSummaryDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_createRoleResult);
        }
    }

    private sealed class StubStringLocalizer : IStringLocalizer<SharedResource>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["IdentityOperationFailedTitle"] = "Identity operation failed",
            ["identity.duplicate_username"] = "This username already exists."
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out var value) ? value : name, resourceNotFound: !Values.ContainsKey(name));

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Values.Select(pair => new LocalizedString(pair.Key, pair.Value, false));
        }

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture)
        {
            return this;
        }
    }
}

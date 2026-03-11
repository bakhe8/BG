using BG.Application.Contracts.Services;
using BG.Application.Models.Identity;
using BG.Web.Contracts.Identity;
using BG.Web.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BG.Web.Controllers;

[ApiController]
[Route("api/admin/identity")]
public sealed class IdentityAdministrationController : ControllerBase
{
    private readonly IIdentityAdministrationService _identityAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IdentityAdministrationController(
        IIdentityAdministrationService identityAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _identityAdministrationService = identityAdministrationService;
        _localizer = localizer;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _identityAdministrationService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserSummaryDto>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAdministrationService.CreateUserAsync(
            new CreateUserCommand(
                request.Username,
                request.DisplayName,
                request.Email,
                request.RoleIds),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : BadRequest(CreateProblemDetails(result.ErrorCode!));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleSummaryDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _identityAdministrationService.GetRolesAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpPost("roles")]
    public async Task<ActionResult<RoleSummaryDto>> CreateRole(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityAdministrationService.CreateRoleAsync(
            new CreateRoleCommand(
                request.Name,
                request.Description,
                request.PermissionKeys),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : BadRequest(CreateProblemDetails(result.ErrorCode!));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _identityAdministrationService.GetPermissionsAsync(cancellationToken);
        return Ok(permissions);
    }

    private ProblemDetails CreateProblemDetails(string errorCode)
    {
        var problemDetails = new ProblemDetails
        {
            Title = _localizer["IdentityOperationFailedTitle"],
            Detail = _localizer[errorCode],
            Status = StatusCodes.Status400BadRequest
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        return problemDetails;
    }
}

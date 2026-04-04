using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Requests;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BG.Web.Pages.Requests;

[Authorize(Policy = PermissionPolicyNames.GuaranteeHistory)]
public sealed class HistoryModel : PageModel
{
    private const int DefaultPageSize = 10;
    private readonly IGuaranteeHistoryService _guaranteeHistoryService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public HistoryModel(
        IGuaranteeHistoryService guaranteeHistoryService,
        IStringLocalizer<SharedResource> localizer)
    {
        _guaranteeHistoryService = guaranteeHistoryService;
        _localizer = localizer;
    }

    [FromQuery(Name = "guaranteeNumber")]
    public string? GuaranteeNumber { get; set; }

    [FromQuery(Name = "page")]
    public int? PageNumber { get; set; }

    public PagedResult<GuaranteeEventEntryDto> History { get; private set; } =
        new([], new PageInfoDto(1, DefaultPageSize, 0));

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = _localizer["requests.user_context_invalid"];
            return;
        }

        var permissions = User.FindAll("permission")
            .Select(claim => claim.Value)
            .ToArray();

        var result = await _guaranteeHistoryService.GetGuaranteeHistoryAsync(
            GuaranteeNumber ?? string.Empty,
            userId,
            permissions,
            PageNumber ?? 1,
            DefaultPageSize,
            cancellationToken);

        if (!result.Succeeded)
        {
            ErrorMessage = _localizer[result.ErrorCode!];
            return;
        }

        GuaranteeNumber = GuaranteeNumber?.Trim();
        History = result.Value!;
    }

    public IDictionary<string, string> BuildPageRoute(int pageNumber)
    {
        var route = new Dictionary<string, string>
        {
            ["page"] = pageNumber.ToString()
        };

        if (!string.IsNullOrWhiteSpace(GuaranteeNumber))
        {
            route["guaranteeNumber"] = GuaranteeNumber;
        }

        return route;
    }
}

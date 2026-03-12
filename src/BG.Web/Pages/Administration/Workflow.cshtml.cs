using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Workflow;
using BG.Domain.Workflow;
using BG.Web.Localization;
using BG.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace BG.Web.Pages.Administration;

[Authorize(Policy = PermissionPolicyNames.WorkflowManage)]
public sealed class WorkflowModel : PageModel
{
    private readonly IWorkflowAdministrationService _workflowAdministrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WorkflowModel(
        IWorkflowAdministrationService workflowAdministrationService,
        IStringLocalizer<SharedResource> localizer)
    {
        _workflowAdministrationService = workflowAdministrationService;
        _localizer = localizer;
    }

    public WorkflowAdministrationSnapshotDto Snapshot { get; private set; } = default!;

    public IReadOnlyList<DelegationPolicyOption> DelegationPolicies { get; } =
    [
        new(ApprovalDelegationPolicy.Inherit, "WorkflowDelegationPolicy_Inherit"),
        new(ApprovalDelegationPolicy.AllowDelegation, "WorkflowDelegationPolicy_AllowDelegation"),
        new(ApprovalDelegationPolicy.DirectSignerRequired, "WorkflowDelegationPolicy_DirectSignerRequired")
    ];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddStageAsync(
        Guid definitionId,
        Guid roleId,
        string? customTitle,
        string? customSummary,
        bool? requiresLetterSignature = null,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit,
        CancellationToken cancellationToken = default)
    {
        var result = await _workflowAdministrationService.AddStageAsync(
            new AddWorkflowStageCommand(definitionId, roleId, customTitle, customSummary, requiresLetterSignature, delegationPolicy),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_AddStageSuccess", cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateStageAsync(
        Guid definitionId,
        Guid stageId,
        Guid? roleId,
        string? customTitle,
        string? customSummary,
        bool? requiresLetterSignature = null,
        ApprovalDelegationPolicy delegationPolicy = ApprovalDelegationPolicy.Inherit,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleId = roleId.GetValueOrDefault() == Guid.Empty ? null : roleId;
        var result = await _workflowAdministrationService.UpdateStageAsync(
            new UpdateWorkflowStageCommand(definitionId, stageId, normalizedRoleId, customTitle, customSummary, requiresLetterSignature, delegationPolicy),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_UpdateStageSuccess", cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateGovernanceAsync(
        Guid definitionId,
        ApprovalDelegationPolicy finalSignatureDelegationPolicy = ApprovalDelegationPolicy.Inherit,
        string? delegationAmountThreshold = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _workflowAdministrationService.UpdateGovernanceAsync(
            new UpdateWorkflowGovernanceCommand(definitionId, finalSignatureDelegationPolicy, delegationAmountThreshold),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_UpdateGovernanceSuccess", cancellationToken);
    }

    public async Task<IActionResult> OnPostMoveUpAsync(Guid definitionId, Guid stageId, CancellationToken cancellationToken)
    {
        var result = await _workflowAdministrationService.MoveStageAsync(
            new MoveWorkflowStageCommand(definitionId, stageId, WorkflowStageMoveDirection.Up),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_ReorderStageSuccess", cancellationToken);
    }

    public async Task<IActionResult> OnPostMoveDownAsync(Guid definitionId, Guid stageId, CancellationToken cancellationToken)
    {
        var result = await _workflowAdministrationService.MoveStageAsync(
            new MoveWorkflowStageCommand(definitionId, stageId, WorkflowStageMoveDirection.Down),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_ReorderStageSuccess", cancellationToken);
    }

    public async Task<IActionResult> OnPostRemoveStageAsync(Guid definitionId, Guid stageId, CancellationToken cancellationToken)
    {
        var result = await _workflowAdministrationService.RemoveStageAsync(
            new RemoveWorkflowStageCommand(definitionId, stageId),
            cancellationToken);

        return await CompleteAsync(result, "WorkflowAdmin_RemoveStageSuccess", cancellationToken);
    }

    private async Task<IActionResult> CompleteAsync(
        OperationResult<Guid> result,
        string successMessageResourceKey,
        CancellationToken cancellationToken)
    {
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, _localizer[result.ErrorCode!]);
            await LoadAsync(cancellationToken);
            return Page();
        }

        StatusMessage = _localizer[successMessageResourceKey];
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Snapshot = await _workflowAdministrationService.GetSnapshotAsync(cancellationToken);
    }

    public sealed record DelegationPolicyOption(ApprovalDelegationPolicy Value, string ResourceKey);
}

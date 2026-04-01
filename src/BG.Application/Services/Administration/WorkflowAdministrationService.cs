using System.Globalization;
using BG.Application.Common;
using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Models.Workflow;
using BG.Application.Workflow;

namespace BG.Application.Services;

internal sealed class WorkflowAdministrationService : IWorkflowAdministrationService
{
    private readonly IWorkflowDefinitionRepository _repository;

    public WorkflowAdministrationService(IWorkflowDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkflowAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _repository.ListDefinitionsAsync(cancellationToken);
        var roles = await _repository.ListRolesAsync(cancellationToken);

        return new WorkflowAdministrationSnapshotDto(
            definitions
                .OrderBy(definition => definition.RequestType)
                .ThenBy(definition => definition.GuaranteeCategory)
                .Select(MapDefinition)
                .ToArray(),
            roles
                .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .Select(role => new WorkflowRoleOptionDto(role.Id, role.Name, role.Description))
                .ToArray());
    }

    public async Task<OperationResult<Guid>> UpdateGovernanceAsync(
        UpdateWorkflowGovernanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var definition = await _repository.GetDefinitionByIdAsync(command.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionNotFound);
        }

        if (!TryParseThreshold(command.DelegationAmountThreshold, out var threshold))
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DelegationAmountThresholdInvalid);
        }

        try
        {
            definition.UpdateGovernance(command.FinalSignatureDelegationPolicy, threshold, DateTimeOffset.UtcNow);
        }
        catch (ArgumentOutOfRangeException)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DelegationAmountThresholdInvalid);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(definition.Id);
    }

    public async Task<OperationResult<Guid>> AddStageAsync(AddWorkflowStageCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RoleId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.RoleRequired);
        }

        var definition = await _repository.GetDefinitionByIdAsync(command.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionNotFound);
        }

        var role = await _repository.GetRoleByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.RoleNotFound);
        }

        definition.AddStage(
            role.Id,
            titleResourceKey: null,
            summaryResourceKey: null,
            command.CustomTitle,
            command.CustomSummary,
            requiresLetterSignature: command.RequiresLetterSignature ?? true,
            modifiedAtUtc: DateTimeOffset.UtcNow,
            delegationPolicy: command.DelegationPolicy);

        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(definition.Id);
    }

    public async Task<OperationResult<Guid>> UpdateStageAsync(UpdateWorkflowStageCommand command, CancellationToken cancellationToken = default)
    {
        var definition = await _repository.GetDefinitionByIdAsync(command.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionNotFound);
        }

        if (!command.RoleId.HasValue || command.RoleId.Value == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.RoleRequired);
        }

        var role = await _repository.GetRoleByIdAsync(command.RoleId.Value, cancellationToken);
        if (role is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.RoleNotFound);
        }

        try
        {
            definition.UpdateStage(
                command.StageId,
                command.RoleId,
                command.CustomTitle,
                command.CustomSummary,
                DateTimeOffset.UtcNow,
                command.RequiresLetterSignature,
                command.DelegationPolicy);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.StageNotFound);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(definition.Id);
    }

    public async Task<OperationResult<Guid>> MoveStageAsync(MoveWorkflowStageCommand command, CancellationToken cancellationToken = default)
    {
        var definition = await _repository.GetDefinitionByIdAsync(command.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionNotFound);
        }

        try
        {
            switch (command.Direction)
            {
                case WorkflowStageMoveDirection.Up:
                    definition.MoveStageUp(command.StageId, DateTimeOffset.UtcNow);
                    break;
                case WorkflowStageMoveDirection.Down:
                    definition.MoveStageDown(command.StageId, DateTimeOffset.UtcNow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Direction), command.Direction, null);
            }
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("first", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.StageAlreadyFirst);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("last", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.StageAlreadyLast);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.StageNotFound);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(definition.Id);
    }

    public async Task<OperationResult<Guid>> RemoveStageAsync(RemoveWorkflowStageCommand command, CancellationToken cancellationToken = default)
    {
        var definition = await _repository.GetDefinitionByIdAsync(command.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionNotFound);
        }

        if (definition.Stages.Count <= 1)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.DefinitionRequiresStage);
        }

        try
        {
            definition.RemoveStage(command.StageId, DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<Guid>.Failure(WorkflowErrorCodes.StageNotFound);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(definition.Id);
    }

    private static WorkflowDefinitionAdminDto MapDefinition(BG.Domain.Workflow.RequestWorkflowDefinition definition)
    {
        return new WorkflowDefinitionAdminDto(
            definition.Id,
            definition.Key,
            definition.RequestType,
            definition.GuaranteeCategory,
            definition.GuaranteeCategoryResourceKey,
            definition.TitleResourceKey,
            definition.SummaryResourceKey,
            definition.IsActive,
            definition.GetIntegrityIssueResourceKeys(),
            definition.Stages
                .OrderBy(stage => stage.Sequence)
                .Select(stage => new WorkflowStageAdminDto(
                    stage.Id,
                    stage.Sequence,
                    stage.RoleId,
                    stage.Role?.Name,
                    stage.TitleResourceKey,
                    stage.SummaryResourceKey,
                    stage.CustomTitle,
                    stage.CustomSummary,
                    stage.RequiresLetterSignature,
                    stage.DelegationPolicy))
                .ToArray(),
            definition.FinalSignatureDelegationPolicy,
            definition.DelegationAmountThreshold);
    }

    private static bool TryParseThreshold(string? value, out decimal? threshold)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            threshold = null;
            return true;
        }

        if (!decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) &&
            !decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            threshold = null;
            return false;
        }

        threshold = parsed;
        return true;
    }
}

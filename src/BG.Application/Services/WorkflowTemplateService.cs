using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Operations;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;

namespace BG.Application.Services;

internal sealed class WorkflowTemplateService : IWorkflowTemplateService
{
    private readonly IWorkflowDefinitionRepository _repository;

    public WorkflowTemplateService(IWorkflowDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RequestWorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _repository.ListDefinitionsAsync(cancellationToken);

        return definitions
            .Where(definition => definition.IsActive && definition.IsOperationallyReady)
            .OrderBy(definition => definition.RequestType)
            .ThenBy(definition => definition.GuaranteeCategory)
            .Select(MapTemplate)
            .ToArray();
    }

    public async Task<RequestWorkflowTemplateDto?> GetTemplateAsync(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        CancellationToken cancellationToken = default)
    {
        var definition = guaranteeCategory.HasValue
            ? await _repository.GetDefinitionAsync(requestType, guaranteeCategory, cancellationToken)
            : null;

        if (!IsOperationalDefinition(definition))
        {
            definition = await _repository.GetDefinitionAsync(requestType, guaranteeCategory: null, cancellationToken);
        }

        if (!IsOperationalDefinition(definition))
        {
            return null;
        }

        return MapTemplate(definition!);
    }

    private static RequestWorkflowTemplateDto MapTemplate(RequestWorkflowDefinition definition)
    {
        return new RequestWorkflowTemplateDto(
            definition.Key,
            definition.RequestType,
            definition.GuaranteeCategory,
            definition.GuaranteeCategoryResourceKey,
            definition.TitleResourceKey,
            definition.SummaryResourceKey,
            definition.Stages
                .OrderBy(stage => stage.Sequence)
                .Select(MapStage)
                .ToArray(),
            definition.FinalSignatureDelegationPolicy,
            definition.DelegationAmountThreshold);
    }

    private static RequestWorkflowStageTemplateDto MapStage(RequestWorkflowStageDefinition stage)
    {
        return new RequestWorkflowStageTemplateDto(
            stage.Sequence,
            stage.TitleResourceKey,
            stage.SummaryResourceKey,
            stage.RequiresLetterSignature,
            "WorkflowSignatureMode_ButtonStampedPdf",
            "WorkflowSignatureEffect_FinalLetterPdf",
            stage.DelegationPolicy,
            stage.RoleId,
            stage.Role?.Name,
            string.IsNullOrWhiteSpace(stage.CustomTitle) ? stage.Role?.Name : stage.CustomTitle,
            string.IsNullOrWhiteSpace(stage.CustomSummary) ? stage.Role?.Description : stage.CustomSummary);
    }

    private static bool IsOperationalDefinition(RequestWorkflowDefinition? definition)
    {
        return definition is not null &&
               definition.IsActive &&
               definition.IsOperationallyReady;
    }
}

using BG.Application.Contracts.Persistence;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class WorkflowTemplateServiceTests
{
    [Fact]
    public async Task GetTemplateAsync_returns_category_specific_definition_when_available()
    {
        var genericDefinition = CreateDefinition(GuaranteeRequestType.Extend, guaranteeCategory: null, "WorkflowTemplate_Extend_Title");
        var categoryDefinition = CreateDefinition(GuaranteeRequestType.Extend, GuaranteeCategory.Contract, "WorkflowTemplate_ExtendContract_Title");

        var service = new WorkflowTemplateService(new StubWorkflowDefinitionRepository(genericDefinition, categoryDefinition));

        var template = await service.GetTemplateAsync(GuaranteeRequestType.Extend, GuaranteeCategory.Contract);

        Assert.NotNull(template);
        Assert.Equal("WorkflowTemplate_ExtendContract_Title", template!.TitleResourceKey);
        Assert.Equal(GuaranteeCategory.Contract, template.GuaranteeCategory);
    }

    [Fact]
    public async Task GetTemplateAsync_falls_back_to_generic_definition_when_category_specific_definition_is_missing()
    {
        var genericDefinition = CreateDefinition(GuaranteeRequestType.Extend, guaranteeCategory: null, "WorkflowTemplate_Extend_Title");

        var service = new WorkflowTemplateService(new StubWorkflowDefinitionRepository(genericDefinition));

        var template = await service.GetTemplateAsync(GuaranteeRequestType.Extend, GuaranteeCategory.Contract);

        Assert.NotNull(template);
        Assert.Equal("WorkflowTemplate_Extend_Title", template!.TitleResourceKey);
        Assert.Null(template.GuaranteeCategory);
    }

    [Fact]
    public async Task GetTemplateAsync_ignores_inactive_or_incomplete_definitions()
    {
        var invalidSpecificDefinition = new RequestWorkflowDefinition(
            "Extend:Contract",
            GuaranteeRequestType.Extend,
            GuaranteeCategory.Contract,
            "GuaranteeCategory_Contract",
            "WorkflowTemplate_ExtendContract_Title",
            "Workflow summary",
            DateTimeOffset.UtcNow);
        invalidSpecificDefinition.AddStage(
            roleId: null,
            titleResourceKey: "WorkflowStage_GuaranteesSupervisor_Title",
            summaryResourceKey: "WorkflowStage_GuaranteesSupervisor_Summary",
            customTitle: null,
            customSummary: null,
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);

        var genericDefinition = CreateDefinition(GuaranteeRequestType.Extend, guaranteeCategory: null, "WorkflowTemplate_Extend_Title");
        var service = new WorkflowTemplateService(new StubWorkflowDefinitionRepository(invalidSpecificDefinition, genericDefinition));

        var template = await service.GetTemplateAsync(GuaranteeRequestType.Extend, GuaranteeCategory.Contract);

        Assert.NotNull(template);
        Assert.Equal("WorkflowTemplate_Extend_Title", template!.TitleResourceKey);
    }

    private static RequestWorkflowDefinition CreateDefinition(
        GuaranteeRequestType requestType,
        GuaranteeCategory? guaranteeCategory,
        string titleResourceKey)
    {
        var definition = new RequestWorkflowDefinition(
            guaranteeCategory is null ? requestType.ToString() : $"{requestType}:{guaranteeCategory}",
            requestType,
            guaranteeCategory,
            guaranteeCategory is null ? null : $"GuaranteeCategory_{guaranteeCategory}",
            titleResourceKey,
            "Workflow summary",
            DateTimeOffset.UtcNow);
        var role = new Role("Approver", "Approver role");

        definition.AddStage(
            role.Id,
            null,
            null,
            "Approver",
            "Approver summary",
            requiresLetterSignature: true,
            modifiedAtUtc: DateTimeOffset.UtcNow);

        typeof(RequestWorkflowStageDefinition)
            .GetProperty(
                nameof(RequestWorkflowStageDefinition.Role),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(definition.Stages.Single(), role);

        return definition;
    }

    private sealed class StubWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly IReadOnlyList<RequestWorkflowDefinition> _definitions;

        public StubWorkflowDefinitionRepository(params RequestWorkflowDefinition[] definitions)
        {
            _definitions = definitions;
        }

        public Task<IReadOnlyList<RequestWorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_definitions);
        }

        public Task<RequestWorkflowDefinition?> GetDefinitionByIdAsync(Guid definitionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_definitions.SingleOrDefault(definition => definition.Id == definitionId));
        }

        public Task<RequestWorkflowDefinition?> GetDefinitionAsync(
            GuaranteeRequestType requestType,
            GuaranteeCategory? guaranteeCategory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_definitions.SingleOrDefault(definition =>
                definition.RequestType == requestType &&
                definition.GuaranteeCategory == guaranteeCategory));
        }

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Role>>([]);
        }

        public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Role?>(null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

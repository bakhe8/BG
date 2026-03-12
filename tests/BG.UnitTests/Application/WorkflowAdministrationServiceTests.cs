using BG.Application.Contracts.Persistence;
using BG.Application.Models.Workflow;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Workflow;

namespace BG.UnitTests.Application;

public sealed class WorkflowAdministrationServiceTests
{
    [Fact]
    public async Task AddStageAsync_appends_stage_and_reindexes_sequence()
    {
        var definition = CreateDefinition();
        var existingRole = CreateRole("Existing");
        definition.AddStage(existingRole.Id, null, null, "Existing stage", null, true, DateTimeOffset.UtcNow);

        var repository = new StubWorkflowDefinitionRepository(definition, existingRole, CreateRole("Supervisor"));
        var service = new WorkflowAdministrationService(repository);
        var role = repository.Roles.Single(item => item.Name == "Supervisor");

        var result = await service.AddStageAsync(
            new AddWorkflowStageCommand(definition.Id, role.Id, "Custom approval", "Extra stage", true));

        Assert.True(result.Succeeded);
        Assert.Equal(2, definition.Stages.Count);
        Assert.Equal(2, definition.Stages.Max(stage => stage.Sequence));
        Assert.True(repository.SaveChangesCalled);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public async Task MoveStageAsync_moves_stage_up()
    {
        var definition = CreateDefinition();
        var firstRole = CreateRole("First");
        var secondRole = CreateRole("Second");
        definition.AddStage(firstRole.Id, null, null, "Stage A", null, true, DateTimeOffset.UtcNow);
        definition.AddStage(secondRole.Id, null, null, "Stage B", null, true, DateTimeOffset.UtcNow);

        var repository = new StubWorkflowDefinitionRepository(definition, firstRole, secondRole);
        var service = new WorkflowAdministrationService(repository);
        var stageToMove = definition.Stages.Single(stage => stage.CustomTitle == "Stage B");

        var result = await service.MoveStageAsync(
            new MoveWorkflowStageCommand(definition.Id, stageToMove.Id, WorkflowStageMoveDirection.Up));

        Assert.True(result.Succeeded);
        Assert.Equal("Stage B", definition.Stages.OrderBy(stage => stage.Sequence).First().CustomTitle);
    }

    [Fact]
    public async Task GetSnapshotAsync_returns_roles_and_stage_assignments()
    {
        var role = CreateRole("Workflow Role");
        var definition = CreateDefinition();
        definition.AddStage(
            role.Id,
            null,
            null,
            "Assigned stage",
            "Summary",
            true,
            DateTimeOffset.UtcNow,
            ApprovalDelegationPolicy.DirectSignerRequired);

        var service = new WorkflowAdministrationService(new StubWorkflowDefinitionRepository(definition, role));

        var snapshot = await service.GetSnapshotAsync();

        Assert.Single(snapshot.Definitions);
        Assert.Single(snapshot.AvailableRoles);
        Assert.Equal(role.Name, snapshot.Definitions[0].Stages[0].RoleName);
        Assert.True(snapshot.Definitions[0].IsActive);
        Assert.Empty(snapshot.Definitions[0].IntegrityIssueResourceKeys);
        Assert.Equal(ApprovalDelegationPolicy.DirectSignerRequired, snapshot.Definitions[0].Stages[0].DelegationPolicy);
    }

    [Fact]
    public async Task UpdateStageAsync_updates_stage_delegation_policy()
    {
        var definition = CreateDefinition();
        var role = CreateRole("Workflow Role");
        definition.AddStage(role.Id, null, null, "Assigned stage", "Summary", true, DateTimeOffset.UtcNow);
        var stage = Assert.Single(definition.Stages);
        var service = new WorkflowAdministrationService(new StubWorkflowDefinitionRepository(definition, role));

        var result = await service.UpdateStageAsync(
            new UpdateWorkflowStageCommand(
                definition.Id,
                stage.Id,
                role.Id,
                "Edited",
                "Edited summary",
                false,
                ApprovalDelegationPolicy.DirectSignerRequired));

        Assert.True(result.Succeeded);
        Assert.Equal(ApprovalDelegationPolicy.DirectSignerRequired, stage.DelegationPolicy);
        Assert.False(stage.RequiresLetterSignature);
    }

    [Fact]
    public async Task UpdateGovernanceAsync_updates_definition_governance_settings()
    {
        var definition = CreateDefinition();
        var role = CreateRole("Workflow Role");
        definition.AddStage(role.Id, null, null, "Assigned stage", "Summary", true, DateTimeOffset.UtcNow);
        var service = new WorkflowAdministrationService(new StubWorkflowDefinitionRepository(definition, role));

        var result = await service.UpdateGovernanceAsync(
            new UpdateWorkflowGovernanceCommand(
                definition.Id,
                ApprovalDelegationPolicy.DirectSignerRequired,
                "250000"));

        Assert.True(result.Succeeded);
        Assert.Equal(ApprovalDelegationPolicy.DirectSignerRequired, definition.FinalSignatureDelegationPolicy);
        Assert.Equal(250000m, definition.DelegationAmountThreshold);
    }

    [Fact]
    public async Task UpdateStageAsync_requires_role_assignment()
    {
        var definition = CreateDefinition();
        var role = CreateRole("Workflow Role");
        definition.AddStage(role.Id, null, null, "Assigned stage", "Summary", true, DateTimeOffset.UtcNow);
        var stage = Assert.Single(definition.Stages);
        var service = new WorkflowAdministrationService(new StubWorkflowDefinitionRepository(definition, role));

        var result = await service.UpdateStageAsync(
            new UpdateWorkflowStageCommand(definition.Id, stage.Id, null, "Edited", "Edited summary"));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Workflow.WorkflowErrorCodes.RoleRequired, result.ErrorCode);
    }

    [Fact]
    public async Task RemoveStageAsync_rejects_removing_last_stage()
    {
        var definition = CreateDefinition();
        var role = CreateRole("Workflow Role");
        definition.AddStage(role.Id, null, null, "Assigned stage", "Summary", true, DateTimeOffset.UtcNow);
        var stage = Assert.Single(definition.Stages);
        var service = new WorkflowAdministrationService(new StubWorkflowDefinitionRepository(definition, role));

        var result = await service.RemoveStageAsync(new RemoveWorkflowStageCommand(definition.Id, stage.Id));

        Assert.False(result.Succeeded);
        Assert.Equal(BG.Application.Workflow.WorkflowErrorCodes.DefinitionRequiresStage, result.ErrorCode);
    }

    private static RequestWorkflowDefinition CreateDefinition()
    {
        return new RequestWorkflowDefinition(
            GuaranteeRequestType.Extend.ToString(),
            GuaranteeRequestType.Extend,
            guaranteeCategory: null,
            guaranteeCategoryResourceKey: null,
            "WorkflowTemplate_Extend_Title",
            "WorkflowTemplate_Extend_Summary",
            DateTimeOffset.UtcNow);
    }

    private static Role CreateRole(string name)
    {
        return new Role(name, $"{name} description");
    }

    private sealed class StubWorkflowDefinitionRepository : IWorkflowDefinitionRepository
    {
        private readonly IReadOnlyList<RequestWorkflowDefinition> _definitions;

        public StubWorkflowDefinitionRepository(RequestWorkflowDefinition definition, params Role[] roles)
        {
            _definitions = [definition];
            Roles = roles;

            foreach (var stage in definition.Stages)
            {
                if (stage.RoleId.HasValue)
                {
                    typeof(RequestWorkflowStageDefinition)
                        .GetProperty(
                            nameof(RequestWorkflowStageDefinition.Role),
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
                        .SetValue(stage, roles.Single(role => role.Id == stage.RoleId.Value));
                }
            }
        }

        public IReadOnlyList<Role> Roles { get; }

        public bool SaveChangesCalled { get; private set; }

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
            return Task.FromResult(Roles);
        }

        public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Roles.SingleOrDefault(role => role.Id == roleId));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;

            foreach (var definition in _definitions)
            {
                foreach (var stage in definition.Stages)
                {
                    var role = stage.RoleId.HasValue
                        ? Roles.SingleOrDefault(item => item.Id == stage.RoleId.Value)
                        : null;

                    typeof(RequestWorkflowStageDefinition)
                        .GetProperty(
                            nameof(RequestWorkflowStageDefinition.Role),
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
                        .SetValue(stage, role);
                }
            }

            return Task.CompletedTask;
        }
    }
}

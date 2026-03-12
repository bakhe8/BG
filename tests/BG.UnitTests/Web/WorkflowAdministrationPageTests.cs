using BG.Application.Common;
using BG.Application.Contracts.Services;
using BG.Application.Models.Workflow;
using BG.Domain.Guarantees;
using BG.Domain.Workflow;
using BG.Web.Localization;
using BG.Web.Pages.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BG.UnitTests.Web;

public sealed class WorkflowAdministrationPageTests
{
    [Fact]
    public async Task OnGetAsync_loads_workflow_snapshot()
    {
        var model = new WorkflowModel(new StubWorkflowAdministrationService(), new PassThroughLocalizer());

        await model.OnGetAsync(CancellationToken.None);

        Assert.Single(model.Snapshot.Definitions);
        Assert.Single(model.Snapshot.AvailableRoles);
    }

    [Fact]
    public async Task OnPostAddStageAsync_redirects_on_success()
    {
        var service = new StubWorkflowAdministrationService();
        var definitionId = service.Snapshot.Definitions[0].Id;
        var roleId = service.Snapshot.AvailableRoles[0].Id;
        var model = new WorkflowModel(service, new PassThroughLocalizer());

        var result = await model.OnPostAddStageAsync(
            definitionId,
            roleId,
            "Extra",
            "Stage",
            true,
            ApprovalDelegationPolicy.DirectSignerRequired,
            CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(model.StatusMessage);
    }

    private sealed class StubWorkflowAdministrationService : IWorkflowAdministrationService
    {
        public StubWorkflowAdministrationService()
        {
            var role = new WorkflowRoleOptionDto(Guid.NewGuid(), "Supervisor", "Signs the letter");
            Snapshot = new WorkflowAdministrationSnapshotDto(
                [
                    new WorkflowDefinitionAdminDto(
                        Guid.NewGuid(),
                        GuaranteeRequestType.Extend.ToString(),
                        GuaranteeRequestType.Extend,
                        null,
                        null,
                        "WorkflowTemplate_Extend_Title",
                        "WorkflowTemplate_Extend_Summary",
                        true,
                        Array.Empty<string>(),
                        [
                            new WorkflowStageAdminDto(
                                Guid.NewGuid(),
                                1,
                                role.Id,
                                role.Name,
                                "WorkflowStage_GuaranteesSupervisor_Title",
                                "WorkflowStage_GuaranteesSupervisor_Summary",
                                null,
                                null,
                                true)
                        ])
                ],
                [role]);
        }

        public WorkflowAdministrationSnapshotDto Snapshot { get; }

        public Task<WorkflowAdministrationSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Snapshot);
        }

        public Task<OperationResult<Guid>> UpdateGovernanceAsync(UpdateWorkflowGovernanceCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DefinitionId));
        }

        public Task<OperationResult<Guid>> AddStageAsync(AddWorkflowStageCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DefinitionId));
        }

        public Task<OperationResult<Guid>> UpdateStageAsync(UpdateWorkflowStageCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DefinitionId));
        }

        public Task<OperationResult<Guid>> MoveStageAsync(MoveWorkflowStageCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DefinitionId));
        }

        public Task<OperationResult<Guid>> RemoveStageAsync(RemoveWorkflowStageCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Guid>.Success(command.DefinitionId));
        }
    }

    private sealed class PassThroughLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

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

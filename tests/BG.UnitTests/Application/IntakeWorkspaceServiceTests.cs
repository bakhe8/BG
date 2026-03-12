using BG.Application.Contracts.Persistence;
using BG.Application.Contracts.Services;
using BG.Application.Intake;
using BG.Application.Services;
using BG.Domain.Guarantees;
using BG.Domain.Identity;
using BG.Domain.Operations;

namespace BG.UnitTests.Application;

public sealed class IntakeWorkspaceServiceTests
{
    [Fact]
    public async Task GetWorkspaceAsync_returns_new_guarantee_as_default_scenario()
    {
        var actor = CreateActor();
        IIntakeWorkspaceService service = new IntakeWorkspaceService(new StubIntakeRepository(actor));

        var workspace = await service.GetWorkspaceAsync();

        Assert.True(workspace.HasEligibleActor);
        Assert.Equal(actor.Id, workspace.ActiveActor!.Id);
        Assert.Equal(IntakeScenarioKeys.NewGuarantee, workspace.SelectedScenario.Key);
        Assert.False(workspace.SelectedScenario.RequiresExistingGuarantee);
        Assert.Contains("IntakeAction_Scan", workspace.AllowedActionKeys);
        Assert.Contains("IntakeExcluded_Approvals", workspace.ExcludedActionKeys);
        Assert.Equal("IntakeWorkspace_PrimaryRole", workspace.PrimaryRoleResourceKey);
    }

    [Fact]
    public async Task GetWorkspaceAsync_accepts_known_scenario_key()
    {
        var actor = CreateActor();
        IIntakeWorkspaceService service = new IntakeWorkspaceService(new StubIntakeRepository(actor));

        var workspace = await service.GetWorkspaceAsync(actor.Id, IntakeScenarioKeys.ExtensionConfirmation);

        Assert.Equal(IntakeScenarioKeys.ExtensionConfirmation, workspace.SelectedScenario.Key);
        Assert.True(workspace.SelectedScenario.RequiresConfirmedExpiryDate);
        Assert.Contains(
            workspace.SelectedScenario.RequiredReviewFieldKeys,
            field => field == IntakeFieldKeys.NewExpiryDate);
    }

    [Fact]
    public async Task GetWorkspaceAsync_returns_no_eligible_actor_when_repository_has_none()
    {
        IIntakeWorkspaceService service = new IntakeWorkspaceService(new StubIntakeRepository());

        var workspace = await service.GetWorkspaceAsync();

        Assert.False(workspace.HasEligibleActor);
        Assert.Null(workspace.ActiveActor);
        Assert.Equal("IntakeWorkspace_NoEligibleActor", workspace.ContextNoticeResourceKey);
    }

    private static User CreateActor()
    {
        var role = new Role("Intake Specialist", "Intake role");
        role.AssignPermissions(
        [
            new Permission("intake.view", "Intake"),
            new Permission("intake.verify", "Intake"),
            new Permission("intake.finalize", "Intake")
        ]);

        var actor = new User(
            "intake.specialist",
            "Intake Specialist",
            "intake.specialist@bg.local",
            externalId: null,
            UserSourceType.Local,
            isActive: true,
            createdAtUtc: DateTimeOffset.UtcNow);
        actor.AssignRoles([role]);
        return actor;
    }

    private sealed class StubIntakeRepository : IIntakeRepository
    {
        private readonly IReadOnlyList<User> _actors;

        public StubIntakeRepository(params User[] actors)
        {
            _actors = actors;
        }

        public Task<IReadOnlyList<User>> ListIntakeActorsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors);
        }

        public Task<User?> GetIntakeActorByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_actors.SingleOrDefault(actor => actor.Id == userId));
        }

        public Task<bool> GuaranteeNumberExistsAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Guarantee?> GetGuaranteeByNumberAsync(string guaranteeNumber, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddGuaranteeAsync(Guarantee guarantee, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddOperationsReviewItemAsync(OperationsReviewItem reviewItem, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

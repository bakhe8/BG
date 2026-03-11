using BG.Application.Contracts.Services;
using BG.Application.Services;

namespace BG.UnitTests.Application;

public sealed class IntakeWorkspaceServiceTests
{
    [Fact]
    public void GetWorkspace_returns_new_guarantee_as_default_scenario()
    {
        IIntakeWorkspaceService service = new IntakeWorkspaceService();

        var workspace = service.GetWorkspace();

        Assert.Equal("new-guarantee", workspace.SelectedScenario.Key);
        Assert.Contains("IntakeAction_Scan", workspace.AllowedActionKeys);
        Assert.Contains("IntakeExcluded_Approvals", workspace.ExcludedActionKeys);
        Assert.Equal("IntakeWorkspace_PrimaryRole", workspace.PrimaryRoleResourceKey);
    }

    [Fact]
    public void GetWorkspace_accepts_known_scenario_key()
    {
        IIntakeWorkspaceService service = new IntakeWorkspaceService();

        var workspace = service.GetWorkspace("extension-confirmation");

        Assert.Equal("extension-confirmation", workspace.SelectedScenario.Key);
        Assert.Contains(
            workspace.SelectedScenario.RequiredReviewFieldKeys,
            field => field == "IntakeField_NewExpiryDate");
    }
}

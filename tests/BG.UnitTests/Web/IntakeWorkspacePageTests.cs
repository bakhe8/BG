using BG.Application.Contracts.Services;
using BG.Application.Services;
using BG.Web.Pages.Intake;

namespace BG.UnitTests.Web;

public sealed class IntakeWorkspacePageTests
{
    [Fact]
    public void OnGet_uses_requested_scenario_when_available()
    {
        var model = CreateModel();
        model.Scenario = "release-confirmation";

        model.OnGet();

        Assert.Equal("release-confirmation", model.SelectedScenario.Key);
    }

    [Fact]
    public void OnGet_falls_back_to_default_scenario_for_unknown_keys()
    {
        var model = CreateModel();
        model.Scenario = "unknown";

        model.OnGet();

        Assert.Equal("new-guarantee", model.SelectedScenario.Key);
    }

    private static WorkspaceModel CreateModel()
    {
        return new WorkspaceModel(new IntakeWorkspaceService());
    }
}

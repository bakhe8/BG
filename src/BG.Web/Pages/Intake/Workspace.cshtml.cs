using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BG.Web.Pages.Intake;

public sealed class WorkspaceModel : PageModel
{
    private readonly IIntakeWorkspaceService _intakeWorkspaceService;

    public WorkspaceModel(IIntakeWorkspaceService intakeWorkspaceService)
    {
        _intakeWorkspaceService = intakeWorkspaceService;
    }

    [FromQuery(Name = "scenario")]
    public string? Scenario { get; set; }

    public IntakeWorkspaceSnapshotDto Workspace { get; private set; } = default!;

    public IntakeScenarioSnapshotDto SelectedScenario { get; private set; } = default!;

    public void OnGet()
    {
        Workspace = _intakeWorkspaceService.GetWorkspace(Scenario);
        SelectedScenario = Workspace.SelectedScenario;
    }
}

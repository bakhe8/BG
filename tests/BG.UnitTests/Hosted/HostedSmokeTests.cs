namespace BG.UnitTests.Hosted;

public sealed class HostedSmokeTests
{
    private readonly HostedFlowTests _flows = new();

    [Fact]
    public Task Authentication_redirect_and_return_flow_survives_smoke()
        => _flows.Protected_workspace_redirects_to_sign_in_and_sign_in_returns_to_requested_page();

    [Fact]
    public Task Home_dashboard_loads_operational_snapshot_in_smoke()
        => _flows.Signed_in_home_page_shows_operational_dashboard_instead_of_framework_metadata();

    [Fact]
    public Task Intake_workspace_loads_in_smoke()
        => _flows.Intake_workspace_loads_minimal_execution_surface_for_signed_in_actor();

    [Fact]
    public Task Operations_queue_loads_in_smoke()
        => _flows.Operations_queue_loads_open_review_items_for_signed_in_actor();

    [Fact]
    public Task Dispatch_workspace_loads_in_smoke()
        => _flows.Dispatch_workspace_loads_ready_requests_for_signed_in_actor();

    [Fact]
    public Task Workflow_administration_round_trip_survives_smoke()
        => _flows.Workflow_administration_enforces_authorization_and_updates_stage_over_http();
}

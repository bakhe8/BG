using BG.Application.Security;

namespace BG.UnitTests.Application;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void PermissionCatalog_includes_dedicated_document_intake_permissions()
    {
        var definitions = PermissionCatalog.Definitions;

        Assert.Contains(definitions, definition => definition.Key == "intake.view" && definition.Area == "Intake");
        Assert.Contains(definitions, definition => definition.Key == "intake.scan" && definition.Area == "Intake");
        Assert.Contains(definitions, definition => definition.Key == "intake.verify" && definition.Area == "Intake");
        Assert.Contains(definitions, definition => definition.Key == "intake.finalize" && definition.Area == "Intake");
    }

    [Fact]
    public void PermissionCatalog_includes_dedicated_dispatch_permissions()
    {
        var definitions = PermissionCatalog.Definitions;

        Assert.Contains(definitions, definition => definition.Key == "dispatch.view" && definition.Area == "Dispatch");
        Assert.Contains(definitions, definition => definition.Key == "dispatch.print" && definition.Area == "Dispatch");
        Assert.Contains(definitions, definition => definition.Key == "dispatch.record" && definition.Area == "Dispatch");
        Assert.Contains(definitions, definition => definition.Key == "dispatch.email" && definition.Area == "Dispatch");
    }
}

namespace BG.UnitTests.Hosted;

/// <summary>
/// Hosted tests spin up full ASP.NET Core hosts (WebApplicationFactory).
/// Running them in parallel exhausts RAM and causes cross-host interference.
/// This collection forces sequential execution.
/// </summary>
[CollectionDefinition("HostedTests", DisableParallelization = true)]
public sealed class HostedTestCollection;

namespace BG.Web.Contracts.System;

public sealed record SystemPingResponse(
    string Status,
    string Environment,
    DateTimeOffset UtcNow);

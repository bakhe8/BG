namespace BG.Web.UI;

public sealed record SurfaceQueueItemLinkModel(
    string PagePath,
    IDictionary<string, string> RouteValues,
    string Subtitle,
    string Title,
    string Summary,
    string Status,
    string MetaPrimary,
    string? MetaSecondary = null,
    bool IsActive = false);

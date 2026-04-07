using System.Collections.Generic;

namespace BG.Web.UI;

public sealed class SurfaceUnifiedLink
{
    public SurfaceUnifiedLink(
        string pagePath,
        IDictionary<string, string> routeValues,
        string subtitle,
        string title,
        string summary,
        string status,
        string metaPrimary,
        string? metaSecondary = null,
        bool isActive = false)
    {
        PagePath = pagePath;
        RouteValues = routeValues;
        Subtitle = subtitle;
        Title = title;
        Summary = summary;
        Status = status;
        MetaPrimary = metaPrimary;
        MetaSecondary = metaSecondary;
        IsActive = isActive;
    }

    public string PagePath { get; }
    public IDictionary<string, string> RouteValues { get; }
    public string Subtitle { get; }
    public string Title { get; }
    public string Summary { get; }
    public string Status { get; }
    public string MetaPrimary { get; }
    public string? MetaSecondary { get; }
    public bool IsActive { get; }
}

namespace BG.Web.UI;

public sealed record SurfaceStatItem(
    string Label,
    object Value,
    string? Summary = null);

public sealed record SurfaceStatStripModel(
    IReadOnlyList<SurfaceStatItem> Items,
    string? CssClass = null);

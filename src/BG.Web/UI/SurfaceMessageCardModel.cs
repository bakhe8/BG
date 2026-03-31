namespace BG.Web.UI;

public sealed record SurfaceMessageCardModel(
    string Title,
    string Summary,
    string? CssClass = null);

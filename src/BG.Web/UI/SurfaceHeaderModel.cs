namespace BG.Web.UI;

public sealed record SurfaceHeaderModel(
    string Eyebrow,
    string Title,
    string? Summary = null,
    IReadOnlyList<string>? Pills = null,
    string? CssClass = null);

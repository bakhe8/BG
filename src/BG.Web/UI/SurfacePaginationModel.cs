namespace BG.Web.UI;

public sealed record SurfacePaginationModel(
    string PagePath,
    IDictionary<string, string>? PreviousRouteValues,
    IDictionary<string, string>? NextRouteValues,
    string PreviousLabel,
    string NextLabel);

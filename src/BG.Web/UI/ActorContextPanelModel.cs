namespace BG.Web.UI;

public enum ActorContextPanelMode
{
    EmptyState,
    LockedPills,
    SelectorForm,
    SelectorField
}

public sealed record ActorContextOptionModel(
    string Value,
    string Label,
    bool Selected = false);

public sealed record SurfaceHiddenFieldModel(
    string Name,
    string? Value);

public sealed record ActorContextPanelModel(
    string Title,
    string? Summary,
    ActorContextPanelMode Mode,
    string? CssClass = null)
{
    public string? EmptyMessage { get; init; }

    public IReadOnlyList<string> Pills { get; init; } = Array.Empty<string>();

    public string? FormAction { get; init; }

    public string FormMethod { get; init; } = "get";

    public string? ExternalFormId { get; init; }

    public string ActorFieldLabel { get; init; } = string.Empty;

    public string ActorFieldName { get; init; } = "actor";

    public string ActorFieldId { get; init; } = "actor";

    public string? SubmitLabel { get; init; }

    public IReadOnlyList<SurfaceHiddenFieldModel> HiddenFields { get; init; } = Array.Empty<SurfaceHiddenFieldModel>();

    public IReadOnlyList<ActorContextOptionModel> Options { get; init; } = Array.Empty<ActorContextOptionModel>();
}

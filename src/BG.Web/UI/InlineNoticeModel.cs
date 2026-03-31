namespace BG.Web.UI;

public sealed record InlineNoticeModel(
    string Title,
    string Summary,
    string ToneClass = "is-info");

namespace BG.Application.Common;

public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, PageInfoDto pageInfo)
    {
        Items = items;
        PageInfo = pageInfo;
    }

    public IReadOnlyList<T> Items { get; }

    public PageInfoDto PageInfo { get; }
}

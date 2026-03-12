namespace BG.Application.Common;

public sealed class PageInfoDto
{
    public PageInfoDto(int pageNumber, int pageSize, int totalItemCount)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 1 : pageSize;
        TotalItemCount = totalItemCount < 0 ? 0 : totalItemCount;
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalItemCount { get; }

    public int TotalPageCount => Math.Max(1, (int)Math.Ceiling(TotalItemCount / (double)PageSize));

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPageCount;

    public int PreviousPageNumber => HasPreviousPage ? PageNumber - 1 : 1;

    public int NextPageNumber => HasNextPage ? PageNumber + 1 : TotalPageCount;
}

namespace BG.Application.Common;

internal static class WorkspacePaging
{
    internal const int DefaultPageSize = 10;

    internal static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber < 1 ? 1 : pageNumber;
    }
}

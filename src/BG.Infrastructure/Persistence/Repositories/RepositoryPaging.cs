using BG.Application.Common;

namespace BG.Infrastructure.Persistence.Repositories;

internal static class RepositoryPaging
{
    internal static PageInfoDto CreatePageInfo(int pageNumber, int pageSize, int totalItemCount)
    {
        var safePageSize = pageSize < 1 ? 1 : pageSize;
        var totalPageCount = Math.Max(1, (int)Math.Ceiling(totalItemCount / (double)safePageSize));
        var normalizedPageNumber = Math.Min(Math.Max(pageNumber, 1), totalPageCount);

        return new PageInfoDto(normalizedPageNumber, safePageSize, totalItemCount);
    }

    internal static bool RequiresClientSideTemporalOrdering(BgDbContext dbContext)
    {
        return string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);
    }

    internal static IReadOnlyList<T> SlicePage<T>(IEnumerable<T> items, PageInfoDto pageInfo)
    {
        return items
            .Skip((pageInfo.PageNumber - 1) * pageInfo.PageSize)
            .Take(pageInfo.PageSize)
            .ToArray();
    }
}

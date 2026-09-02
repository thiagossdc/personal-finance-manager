using Microsoft.EntityFrameworkCore;

namespace PersonalFinance.Application.Common;

/// <summary>Lista paginada genérica usada em todos os endpoints que retornam coleções.</summary>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PaginatedList(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedList<T>(items, page, pageSize, totalCount);
    }
}

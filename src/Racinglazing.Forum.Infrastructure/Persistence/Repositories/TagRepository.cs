using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Infrastructure.Persistence.Pagination;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class TagRepository(ForumDbContext db) : ITagRepository
{
    public void Add(Tag tag) => db.Tags.Add(tag);

    public async Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await db.Tags.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<IReadOnlyList<Tag>> GetBySlugsAsync(IReadOnlyCollection<string> slugs, CancellationToken ct = default)
    {
        if (slugs.Count == 0) return [];
        return await db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync(ct);
    }

    public async Task<PagedResult<Tag>> SearchAsync(string? search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Tags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, $"{term}%") || EF.Functions.ILike(t.Slug, $"{term}%"));
        }

        // Most popular first; keyset by (threadCount, id).
        var cursor = CursorCodec.Decode<CommentCursor>(page.Cursor);
        if (cursor is not null && long.TryParse(cursor.Key, out var pivotCount))
        {
            query = query.Where(t =>
                t.ThreadCount < pivotCount ||
                (t.ThreadCount == pivotCount && t.Id.CompareTo(cursor.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(t => t.ThreadCount).ThenByDescending(t => t.Id)
            .Take(page.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? next = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new CommentCursor(rows[^1].ThreadCount.ToString(), rows[^1].Id))
            : null;

        return new PagedResult<Tag>(rows, next, hasMore);
    }
}

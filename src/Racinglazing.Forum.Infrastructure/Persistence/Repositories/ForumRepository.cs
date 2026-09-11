using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Infrastructure.Persistence.Pagination;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class ForumRepository(ForumDbContext db) : IForumRepository
{
    public async Task<DomainForum?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Forums.FirstOrDefaultAsync(f => f.Id == id && f.IsActive, ct);

    public async Task<PagedResult<DomainForum>> ListAsync(Guid? categoryId, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Forums.AsNoTracking().Where(f => f.IsActive);
        if (categoryId is { } cid) query = query.Where(f => f.CategoryId == cid);

        // Stable ordering by creation for keyset paging.
        var cursor = CursorCodec.Decode<CreatedCursor>(page.Cursor);
        if (cursor is not null)
        {
            var pivot = new DateTimeOffset(cursor.Ticks, TimeSpan.Zero);
            query = query.Where(f =>
                f.CreatedAt < pivot || (f.CreatedAt == pivot && f.Id.CompareTo(cursor.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(f => f.CreatedAt).ThenByDescending(f => f.Id)
            .Take(page.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? next = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new CreatedCursor(rows[^1].CreatedAt.UtcTicks, rows[^1].Id))
            : null;

        return new PagedResult<DomainForum>(rows, next, hasMore);
    }
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Domain.Enums;
using Racinglazing.Forum.Infrastructure.Persistence.Pagination;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class ThreadRepository(ForumDbContext db) : IThreadRepository
{
    public void Add(DomainThread thread) => db.Threads.Add(thread);

    public async Task<DomainThread?> GetByIdAsync(Guid id, bool includeTags = false, bool includeForum = false, CancellationToken ct = default)
    {
        var query = db.Threads.AsQueryable();
        if (includeForum) query = query.Include(t => t.Forum);
        if (includeTags) query = query.Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag).AsSplitQuery();
        return await query.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<bool> SlugExistsInForumAsync(Guid forumId, string slug, CancellationToken ct = default)
        => await db.Threads.AnyAsync(t => t.ForumId == forumId && t.Slug == slug, ct);

    public Task<PagedResult<DomainThread>> ListByForumAsync(Guid forumId, ThreadSort sort, PageRequest page, CancellationToken ct = default)
        => ListAsync(db.Threads.Where(t => t.ForumId == forumId && !t.IsDeleted), sort, page, ct);

    public Task<PagedResult<DomainThread>> ListByRaceAsync(string raceId, ThreadSort sort, PageRequest page, CancellationToken ct = default)
        => ListAsync(db.Threads.Where(t => t.RaceId == raceId && !t.IsDeleted), sort, page, ct);

    public Task<PagedResult<DomainThread>> ListByTagAsync(Guid tagId, ThreadSort sort, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Threads.Where(t => !t.IsDeleted && t.ThreadTags.Any(tt => tt.TagId == tagId));
        return ListAsync(query, sort, page, ct);
    }

    private static async Task<PagedResult<DomainThread>> ListAsync(
        IQueryable<DomainThread> baseQuery, ThreadSort sort, PageRequest page, CancellationToken ct)
    {
        var cursor = CursorCodec.Decode<ThreadCursor>(page.Cursor);
        var query = ApplyKeyset(baseQuery, sort, cursor);
        query = ApplyOrder(query, sort);

        var rows = await query
            .AsNoTracking()
            .Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag)
            .AsSplitQuery()
            .Take(page.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? next = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new ThreadCursor(rows[^1].IsPinned ? 1 : 0, KeyOf(rows[^1], sort), rows[^1].Id))
            : null;

        return new PagedResult<DomainThread>(rows, next, hasMore);
    }

    // Pinned threads always sort before normal threads, then by the chosen key,
    // then by id as a stable tiebreaker — all descending.
    private static IQueryable<DomainThread> ApplyOrder(IQueryable<DomainThread> q, ThreadSort sort) => sort switch
    {
        ThreadSort.New => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id),
        ThreadSort.Top => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.Score).ThenByDescending(t => t.Id),
        ThreadSort.MostCommented => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CommentCount).ThenByDescending(t => t.Id),
        ThreadSort.RecentlyUpdated => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.LastActivityAt).ThenByDescending(t => t.Id),
        _ => q.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.HotScore).ThenByDescending(t => t.Id) // Hot
    };

    private static IQueryable<DomainThread> ApplyKeyset(IQueryable<DomainThread> q, ThreadSort sort, ThreadCursor? cursor)
    {
        if (cursor is null) return q;
        int cp = cursor.Pinned;
        var id = cursor.Id;

        switch (sort)
        {
            case ThreadSort.New:
            {
                var pivot = new DateTimeOffset(long.Parse(cursor.Key), TimeSpan.Zero);
                return q.Where(t =>
                    (t.IsPinned ? 1 : 0) < cp ||
                    ((t.IsPinned ? 1 : 0) == cp && t.CreatedAt < pivot) ||
                    ((t.IsPinned ? 1 : 0) == cp && t.CreatedAt == pivot && t.Id.CompareTo(id) < 0));
            }
            case ThreadSort.Top:
            {
                var pivot = int.Parse(cursor.Key);
                return q.Where(t =>
                    (t.IsPinned ? 1 : 0) < cp ||
                    ((t.IsPinned ? 1 : 0) == cp && t.Score < pivot) ||
                    ((t.IsPinned ? 1 : 0) == cp && t.Score == pivot && t.Id.CompareTo(id) < 0));
            }
            case ThreadSort.MostCommented:
            {
                var pivot = int.Parse(cursor.Key);
                return q.Where(t =>
                    (t.IsPinned ? 1 : 0) < cp ||
                    ((t.IsPinned ? 1 : 0) == cp && t.CommentCount < pivot) ||
                    ((t.IsPinned ? 1 : 0) == cp && t.CommentCount == pivot && t.Id.CompareTo(id) < 0));
            }
            case ThreadSort.RecentlyUpdated:
            {
                var pivot = new DateTimeOffset(long.Parse(cursor.Key), TimeSpan.Zero);
                return q.Where(t =>
                    (t.IsPinned ? 1 : 0) < cp ||
                    ((t.IsPinned ? 1 : 0) == cp && t.LastActivityAt < pivot) ||
                    ((t.IsPinned ? 1 : 0) == cp && t.LastActivityAt == pivot && t.Id.CompareTo(id) < 0));
            }
            default: // Hot
            {
                var pivot = double.Parse(cursor.Key, CultureInfo.InvariantCulture);
                return q.Where(t =>
                    (t.IsPinned ? 1 : 0) < cp ||
                    ((t.IsPinned ? 1 : 0) == cp && t.HotScore < pivot) ||
                    ((t.IsPinned ? 1 : 0) == cp && t.HotScore == pivot && t.Id.CompareTo(id) < 0));
            }
        }
    }

    private static string KeyOf(DomainThread t, ThreadSort sort) => sort switch
    {
        ThreadSort.New => t.CreatedAt.UtcTicks.ToString(),
        ThreadSort.Top => t.Score.ToString(),
        ThreadSort.MostCommented => t.CommentCount.ToString(),
        ThreadSort.RecentlyUpdated => t.LastActivityAt.UtcTicks.ToString(),
        _ => t.HotScore.ToString("R", CultureInfo.InvariantCulture)
    };
}

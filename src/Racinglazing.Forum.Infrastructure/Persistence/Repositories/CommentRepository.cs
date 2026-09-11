using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using Racinglazing.Forum.Infrastructure.Persistence.Pagination;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class CommentRepository(ForumDbContext db) : ICommentRepository
{
    public void Add(Comment comment) => db.Comments.Add(comment);

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<PagedResult<Comment>> ListTopLevelAsync(Guid threadId, CommentSort sort, PageRequest page, CancellationToken ct = default)
    {
        // Top-level = has no parent and is not the thread's root (body) comment.
        var query = db.Comments.AsNoTracking()
            .Where(c => c.ThreadId == threadId && c.ParentCommentId == null && !c.IsRoot);
        return PageAsync(query, sort, page, ct);
    }

    public Task<PagedResult<Comment>> ListRepliesAsync(Guid parentCommentId, CommentSort sort, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Comments.AsNoTracking()
            .Where(c => c.ParentCommentId == parentCommentId);
        return PageAsync(query, sort, page, ct);
    }

    private static async Task<PagedResult<Comment>> PageAsync(
        IQueryable<Comment> query, CommentSort sort, PageRequest page, CancellationToken ct)
    {
        var cursor = CursorCodec.Decode<CommentCursor>(page.Cursor);
        query = ApplyKeyset(query, sort, cursor);
        query = ApplyOrder(query, sort);

        var rows = await query.Take(page.PageSize + 1).ToListAsync(ct);
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? next = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new CommentCursor(KeyOf(rows[^1], sort), rows[^1].Id))
            : null;

        return new PagedResult<Comment>(rows, next, hasMore);
    }

    private static IQueryable<Comment> ApplyOrder(IQueryable<Comment> q, CommentSort sort) => sort switch
    {
        CommentSort.New => q.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
        CommentSort.Old => q.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
        CommentSort.Controversial => q.OrderByDescending(c => c.ControversyScore).ThenByDescending(c => c.Id),
        _ => q.OrderByDescending(c => c.Score).ThenByDescending(c => c.Id) // Top
    };

    private static IQueryable<Comment> ApplyKeyset(IQueryable<Comment> q, CommentSort sort, CommentCursor? cursor)
    {
        if (cursor is null) return q;
        var id = cursor.Id;

        switch (sort)
        {
            case CommentSort.New:
            {
                var pivot = new DateTimeOffset(long.Parse(cursor.Key), TimeSpan.Zero);
                return q.Where(c => c.CreatedAt < pivot || (c.CreatedAt == pivot && c.Id.CompareTo(id) < 0));
            }
            case CommentSort.Old:
            {
                var pivot = new DateTimeOffset(long.Parse(cursor.Key), TimeSpan.Zero);
                return q.Where(c => c.CreatedAt > pivot || (c.CreatedAt == pivot && c.Id.CompareTo(id) > 0));
            }
            case CommentSort.Controversial:
            {
                var pivot = double.Parse(cursor.Key, CultureInfo.InvariantCulture);
                return q.Where(c => c.ControversyScore < pivot || (c.ControversyScore == pivot && c.Id.CompareTo(id) < 0));
            }
            default: // Top
            {
                var pivot = int.Parse(cursor.Key);
                return q.Where(c => c.Score < pivot || (c.Score == pivot && c.Id.CompareTo(id) < 0));
            }
        }
    }

    private static string KeyOf(Comment c, CommentSort sort) => sort switch
    {
        CommentSort.New or CommentSort.Old => c.CreatedAt.UtcTicks.ToString(),
        CommentSort.Controversial => c.ControversyScore.ToString("R", CultureInfo.InvariantCulture),
        _ => c.Score.ToString()
    };
}

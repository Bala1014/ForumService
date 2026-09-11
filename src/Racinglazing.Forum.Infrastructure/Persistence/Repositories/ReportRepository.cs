using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using Racinglazing.Forum.Infrastructure.Persistence.Pagination;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(ForumDbContext db) : IReportRepository
{
    public void Add(Report report) => db.Reports.Add(report);

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<PagedResult<Report>> ListAsync(
        ReportStatus? status, VoteTargetType? targetType, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Reports.AsNoTracking().AsQueryable();
        if (status is { } s) query = query.Where(r => r.Status == s);
        if (targetType is { } t) query = query.Where(r => r.TargetType == t);

        var cursor = CursorCodec.Decode<CreatedCursor>(page.Cursor);
        if (cursor is not null)
        {
            var pivot = new DateTimeOffset(cursor.Ticks, TimeSpan.Zero);
            query = query.Where(r =>
                r.CreatedAt < pivot || (r.CreatedAt == pivot && r.Id.CompareTo(cursor.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Take(page.PageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? next = hasMore && rows.Count > 0
            ? CursorCodec.Encode(new CreatedCursor(rows[^1].CreatedAt.UtcTicks, rows[^1].Id))
            : null;

        return new PagedResult<Report>(rows, next, hasMore);
    }
}

using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class VoteRepository(ForumDbContext db) : IVoteRepository
{
    public void Add(Vote vote) => db.Votes.Add(vote);
    public void Remove(Vote vote) => db.Votes.Remove(vote);

    public async Task<Vote?> GetAsync(string userId, VoteTargetType targetType, Guid targetId, CancellationToken ct = default)
        => await db.Votes.FirstOrDefaultAsync(
            v => v.UserId == userId && v.TargetType == targetType && v.TargetId == targetId, ct);

    public async Task<IReadOnlyDictionary<Guid, VoteValue>> GetUserVotesAsync(
        string userId, VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, CancellationToken ct = default)
    {
        if (targetIds.Count == 0) return new Dictionary<Guid, VoteValue>();

        return await db.Votes.AsNoTracking()
            .Where(v => v.UserId == userId && v.TargetType == targetType && targetIds.Contains(v.TargetId))
            .ToDictionaryAsync(v => v.TargetId, v => v.Value, ct);
    }
}

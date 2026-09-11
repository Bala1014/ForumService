using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Features.Votes;

public interface IVoteService
{
    Task<VoteResultDto> CastAsync(VoteTargetType targetType, Guid targetId, VoteValue value, CancellationToken ct = default);
    Task<VoteResultDto> RemoveAsync(VoteTargetType targetType, Guid targetId, CancellationToken ct = default);
}

/// <summary>
/// Applies vote deltas to the target's denormalised tally in a single
/// transaction. Because counts live on the target, casting a vote is O(1) and
/// never re-aggregates the votes table.
/// </summary>
public sealed class VoteService(
    IVoteRepository votes,
    IThreadRepository threads,
    ICommentRepository comments,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IVoteService
{
    public async Task<VoteResultDto> CastAsync(VoteTargetType targetType, Guid targetId, VoteValue value, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var target = await LoadVotableAsync(targetType, targetId, ct);

        int up = target.UpvoteCount, down = target.DownvoteCount;
        var existing = await votes.GetAsync(userId, targetType, targetId, ct);

        if (existing is null)
        {
            votes.Add(new Vote(userId, targetType, targetId, value));
            if (value == VoteValue.Up) up++; else down++;
        }
        else if (existing.Value != value)
        {
            existing.Change(value, clock.UtcNow);
            if (value == VoteValue.Up) { up++; down--; } else { up--; down++; }
        }
        // else: same value already recorded — idempotent no-op.

        target.ApplyVoteTotals(Math.Max(0, up), Math.Max(0, down));
        await uow.SaveChangesAsync(ct);

        return new VoteResultDto(targetId.ToString(), value.ToApiString(),
            target.Score, target.UpvoteCount, target.DownvoteCount);
    }

    public async Task<VoteResultDto> RemoveAsync(VoteTargetType targetType, Guid targetId, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var target = await LoadVotableAsync(targetType, targetId, ct);

        var existing = await votes.GetAsync(userId, targetType, targetId, ct);
        if (existing is not null)
        {
            int up = target.UpvoteCount, down = target.DownvoteCount;
            if (existing.Value == VoteValue.Up) up--; else down--;
            votes.Remove(existing);
            target.ApplyVoteTotals(Math.Max(0, up), Math.Max(0, down));
            await uow.SaveChangesAsync(ct);
        }

        return new VoteResultDto(targetId.ToString(), null,
            target.Score, target.UpvoteCount, target.DownvoteCount);
    }

    private async Task<IVotable> LoadVotableAsync(VoteTargetType targetType, Guid targetId, CancellationToken ct)
    {
        IVotable? target = targetType switch
        {
            VoteTargetType.Thread => await threads.GetByIdAsync(targetId, ct: ct),
            VoteTargetType.Comment => await comments.GetByIdAsync(targetId, ct),
            _ => null
        };

        return target ?? throw new NotFoundException(
            targetType == VoteTargetType.Thread ? "THREAD_NOT_FOUND" : "COMMENT_NOT_FOUND",
            "The vote target was not found.");
    }
}

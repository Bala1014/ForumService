using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// A single user's vote on a thread or comment. Polymorphic by
/// (TargetType, TargetId) with a unique constraint on
/// (UserId, TargetType, TargetId) so a user has at most one vote per target.
/// Aggregate counts live denormalised on the target for read speed.
/// </summary>
public class Vote : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public VoteTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public VoteValue Value { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Vote() { }

    public Vote(string userId, VoteTargetType targetType, Guid targetId, VoteValue value)
    {
        UserId = userId;
        TargetType = targetType;
        TargetId = targetId;
        Value = value;
        UpdatedAt = CreatedAt;
    }

    public void Change(VoteValue value, DateTimeOffset at)
    {
        Value = value;
        UpdatedAt = at;
    }
}

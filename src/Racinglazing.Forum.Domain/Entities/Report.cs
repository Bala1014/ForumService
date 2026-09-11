using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// A user report against a thread or comment, plus its moderation resolution.
/// </summary>
public class Report : Entity
{
    public VoteTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }

    public string ReportedByUserId { get; private set; } = string.Empty;
    public ReportReason Reason { get; private set; }
    public string? Description { get; private set; }

    public ReportStatus Status { get; private set; } = ReportStatus.Pending;
    public ModerationActionType Action { get; private set; } = ModerationActionType.None;
    public string? Notes { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }
    public string? ResolvedByUserId { get; private set; }

    private Report() { }

    public Report(VoteTargetType targetType, Guid targetId, string reportedByUserId,
        ReportReason reason, string? description)
    {
        TargetType = targetType;
        TargetId = targetId;
        ReportedByUserId = reportedByUserId;
        Reason = reason;
        Description = description;
    }

    public void Resolve(ReportStatus status, ModerationActionType action, string? notes,
        string resolvedByUserId, DateTimeOffset at)
    {
        Status = status;
        Action = action;
        Notes = notes;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAt = at;
    }
}

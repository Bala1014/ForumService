namespace Racinglazing.Forum.Domain.Enums;

public enum ReportReason
{
    Spam = 0,
    Harassment = 1,
    Hate = 2,
    Misinformation = 3,
    Advertising = 4,
    Other = 5
}

public enum ReportStatus
{
    Pending = 0,
    Reviewing = 1,
    ActionTaken = 2,
    Dismissed = 3
}

/// <summary>
/// The moderation action recorded when a report is resolved. This is an audit
/// value only — actually banning/warning a user is UserService's job.
/// </summary>
public enum ModerationActionType
{
    None = 0,
    ContentRemoved = 1,
    ThreadLocked = 2,
    UserWarned = 3,
    UserBanned = 4
}

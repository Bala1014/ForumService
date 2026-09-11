using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Common;

/// <summary>
/// Maps the contract's camelCase string enums to/from domain enums. Kept in one
/// place so the wire vocabulary is defined once.
/// </summary>
public static class ApiEnums
{
    public static ThreadSort ParseThreadSort(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        null or "" or "hot" => ThreadSort.Hot,
        "new" => ThreadSort.New,
        "top" => ThreadSort.Top,
        "mostcommented" => ThreadSort.MostCommented,
        "recentlyupdated" => ThreadSort.RecentlyUpdated,
        _ => throw Invalid("INVALID_SORT", raw!)
    };

    public static CommentSort ParseCommentSort(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        null or "" or "top" => CommentSort.Top,
        "new" => CommentSort.New,
        "old" => CommentSort.Old,
        "controversial" => CommentSort.Controversial,
        _ => throw Invalid("INVALID_SORT", raw!)
    };

    public static VoteTargetType ParseTargetType(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "thread" => VoteTargetType.Thread,
        "comment" => VoteTargetType.Comment,
        _ => throw Invalid("INVALID_TARGET_TYPE", raw ?? "null")
    };

    public static string ToApiString(this VoteTargetType type) => type switch
    {
        VoteTargetType.Thread => "thread",
        _ => "comment"
    };

    public static ReportReason ParseReason(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "spam" => ReportReason.Spam,
        "harassment" => ReportReason.Harassment,
        "hate" => ReportReason.Hate,
        "misinformation" => ReportReason.Misinformation,
        "advertising" => ReportReason.Advertising,
        "other" => ReportReason.Other,
        _ => throw Invalid("INVALID_REASON", raw ?? "null")
    };

    public static string ToApiString(this ReportReason reason) => reason switch
    {
        ReportReason.Spam => "spam",
        ReportReason.Harassment => "harassment",
        ReportReason.Hate => "hate",
        ReportReason.Misinformation => "misinformation",
        ReportReason.Advertising => "advertising",
        _ => "other"
    };

    public static ReportStatus ParseStatus(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "pending" => ReportStatus.Pending,
        "reviewing" => ReportStatus.Reviewing,
        "actiontaken" => ReportStatus.ActionTaken,
        "dismissed" => ReportStatus.Dismissed,
        _ => throw Invalid("INVALID_STATUS", raw ?? "null")
    };

    public static ReportStatus? ParseStatusOrNull(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : ParseStatus(raw);

    public static string ToApiString(this ReportStatus status) => status switch
    {
        ReportStatus.Pending => "pending",
        ReportStatus.Reviewing => "reviewing",
        ReportStatus.ActionTaken => "actionTaken",
        _ => "dismissed"
    };

    public static ModerationActionType ParseAction(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        null or "" or "none" => ModerationActionType.None,
        "contentremoved" => ModerationActionType.ContentRemoved,
        "threadlocked" => ModerationActionType.ThreadLocked,
        "userwarned" => ModerationActionType.UserWarned,
        "userbanned" => ModerationActionType.UserBanned,
        _ => throw Invalid("INVALID_ACTION", raw!)
    };

    public static string ToApiString(this ModerationActionType action) => action switch
    {
        ModerationActionType.ContentRemoved => "contentRemoved",
        ModerationActionType.ThreadLocked => "threadLocked",
        ModerationActionType.UserWarned => "userWarned",
        ModerationActionType.UserBanned => "userBanned",
        _ => "none"
    };

    private static ValidationFailedException Invalid(string code, string value)
        => new(code, $"'{value}' is not a valid value.");
}

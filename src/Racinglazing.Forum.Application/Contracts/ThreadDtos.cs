namespace Racinglazing.Forum.Application.Contracts;

public sealed record ThreadForumRefDto(string Id, string Name, string Slug);

public sealed record RaceRefDto(string Id);

/// <summary>Thread as it appears in feeds (forum/tag/race listings).</summary>
public sealed record ThreadSummaryDto(
    string Id,
    string ForumId,
    UserRefDto Author,
    string Title,
    string Slug,
    int Score,
    int UpvoteCount,
    int DownvoteCount,
    int CommentCount,
    long ViewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    bool IsPinned,
    bool IsLocked,
    IReadOnlyList<TagDto> Tags,
    string? RaceId,
    string? UserVote);

/// <summary>Full thread detail including the root comment (body).</summary>
public sealed record ThreadDetailDto(
    string Id,
    ThreadForumRefDto Forum,
    UserRefDto Author,
    string Title,
    string Slug,
    int Score,
    int UpvoteCount,
    int DownvoteCount,
    int CommentCount,
    long ViewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset LastActivityAt,
    bool IsPinned,
    bool IsLocked,
    bool IsDeleted,
    string? UserVote,
    IReadOnlyList<TagDto> Tags,
    RaceRefDto? Race,
    CommentDto? RootComment);

public sealed record CreateThreadResultDto(
    string Id,
    string ForumId,
    string AuthorId,
    string Title,
    string Slug,
    string RootCommentId,
    DateTimeOffset CreatedAt);

public sealed record ThreadModerationStateDto(string ThreadId, bool IsPinned);

public sealed record ThreadLockStateDto(string ThreadId, bool IsLocked, DateTimeOffset? LockedAt);

public sealed record ThreadDeleteResultDto(string Id, bool IsDeleted, DateTimeOffset? DeletedAt);

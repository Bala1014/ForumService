namespace Racinglazing.Forum.Application.Contracts;

public sealed record CommentDto(
    string Id,
    string ThreadId,
    string? ParentCommentId,
    UserRefDto Author,
    ContentDto Content,
    int Score,
    int UpvoteCount,
    int DownvoteCount,
    int ReplyCount,
    int Depth,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsEdited,
    bool IsDeleted,
    string? UserVote);

/// <summary>Returned by the create-comment / create-reply endpoints (no author
/// enrichment or userVote — the author just posted it).</summary>
public sealed record CreateCommentResultDto(
    string Id,
    string ThreadId,
    string? ParentCommentId,
    string AuthorId,
    ContentDto Content,
    int Score,
    int UpvoteCount,
    int DownvoteCount,
    int ReplyCount,
    int Depth,
    DateTimeOffset CreatedAt,
    bool IsEdited,
    bool IsDeleted);

namespace Racinglazing.Forum.Application.Contracts;

// Request bodies. Note: authorId is NEVER accepted from the client — it is
// always taken from the authenticated user context.

public sealed record CreateThreadRequest(
    string Title,
    ContentDto Content,
    IReadOnlyList<string>? Tags,
    string? RaceId);

public sealed record CreateCommentRequest(ContentDto Content);

public sealed record VoteRequest(string Vote);

public sealed record LockThreadRequest(string? Reason);

public sealed record CreateReportRequest(
    string TargetType,
    string TargetId,
    string Reason,
    string? Description);

public sealed record ResolveReportRequest(
    string Status,
    string? Action,
    string? Notes);

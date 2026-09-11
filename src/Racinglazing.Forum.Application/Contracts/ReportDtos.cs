namespace Racinglazing.Forum.Application.Contracts;

public sealed record ReportDto(
    string Id,
    string TargetType,
    string TargetId,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ModerationReportDto(
    string Id,
    string TargetType,
    string TargetId,
    UserRefDto ReportedBy,
    string Reason,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record ResolveReportResultDto(
    string Id,
    string Status,
    string Action,
    string? Notes,
    DateTimeOffset? ResolvedAt,
    string? ResolvedBy);

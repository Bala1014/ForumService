namespace Racinglazing.Forum.Application.Contracts;

public sealed record ForumDto(
    string Id,
    string CategoryId,
    string Name,
    string Slug,
    string? Description,
    long ThreadCount,
    long PostCount,
    DateTimeOffset? LastActivityAt);

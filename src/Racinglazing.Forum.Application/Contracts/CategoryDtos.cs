namespace Racinglazing.Forum.Application.Contracts;

public sealed record CategorySummaryDto(
    string Id,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    int ForumCount,
    long ThreadCount);

public sealed record CategoryForumDto(
    string Id,
    string Name,
    string Slug,
    string? Description,
    long ThreadCount,
    long PostCount);

public sealed record CategoryDetailDto(
    string Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyList<CategoryForumDto> Forums);

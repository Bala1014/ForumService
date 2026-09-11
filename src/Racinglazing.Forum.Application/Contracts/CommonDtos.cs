namespace Racinglazing.Forum.Application.Contracts;

/// <summary>Lightweight user representation for response enrichment.
/// ForumService does not own this data.</summary>
public sealed record UserRefDto(string Id, string Username, string DisplayName, string? AvatarUrl);

public sealed record TagDto(string Id, string Name, string Slug);

public sealed record TagSummaryDto(string Id, string Name, string Slug, long ThreadCount);

/// <summary>Comment/thread body. Object-shaped ({ text }) so media can be added later.</summary>
public sealed record ContentDto(string Text);

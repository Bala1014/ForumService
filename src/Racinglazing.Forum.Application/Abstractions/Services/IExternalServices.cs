using Racinglazing.Forum.Application.Contracts;

namespace Racinglazing.Forum.Application.Abstractions.Services;

/// <summary>
/// Resolves lightweight user representations for response enrichment.
/// ForumService does not own user data — this is backed by a stub today and a
/// UserService HTTP client + local read-model cache later, with no change to
/// callers.
/// </summary>
public interface IUserProfileProvider
{
    Task<IReadOnlyDictionary<string, UserRefDto>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct = default);
}

/// <summary>
/// Validates race references against RaceService (the source of truth).
/// Stubbed for the MVP; swap for an HTTP client without touching callers.
/// </summary>
public interface IRaceServiceClient
{
    Task<bool> RaceExistsAsync(string raceId, CancellationToken ct = default);
}

/// <summary>Generates URL-safe slugs from titles/names.</summary>
public interface ISlugGenerator
{
    string Generate(string input);
}

/// <summary>Abstraction over the system clock for testability.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

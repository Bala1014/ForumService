using Microsoft.Extensions.Logging;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Contracts;

namespace Racinglazing.Forum.Infrastructure.Services;

/// <summary>
/// Placeholder user-profile provider used until UserService exists. It derives
/// a lightweight representation from the author id so responses are shaped
/// correctly today. Swap for an HTTP-backed provider (with a local read-model
/// cache) later — callers depend only on <see cref="IUserProfileProvider"/>.
/// </summary>
public sealed class StubUserProfileProvider : IUserProfileProvider
{
    public Task<IReadOnlyDictionary<string, UserRefDto>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, UserRefDto> map = userIds
            .Distinct()
            .ToDictionary(id => id, id => new UserRefDto(id, id, id, null));
        return Task.FromResult(map);
    }
}

/// <summary>
/// Placeholder race validator used until RaceService integration lands. Accepts
/// any non-empty race id. Swap for an HTTP client that verifies existence.
/// </summary>
public sealed class StubRaceServiceClient(ILogger<StubRaceServiceClient> logger) : IRaceServiceClient
{
    public Task<bool> RaceExistsAsync(string raceId, CancellationToken ct = default)
    {
        logger.LogDebug("Stub race validation for {RaceId}; accepting.", raceId);
        return Task.FromResult(!string.IsNullOrWhiteSpace(raceId));
    }
}

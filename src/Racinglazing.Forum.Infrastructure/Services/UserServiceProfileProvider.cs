using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Contracts;

namespace Racinglazing.Forum.Infrastructure.Services;

/// <summary>
/// Resolves stored UserService IDs into the public author information returned
/// by forum endpoints. The batch endpoint avoids one request per thread.
/// </summary>
public sealed class UserServiceProfileProvider(
    HttpClient httpClient,
    ILogger<UserServiceProfileProvider> logger) : IUserProfileProvider
{
    public async Task<IReadOnlyDictionary<string, UserRefDto>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct = default)
    {
        var ids = userIds
            .Distinct()
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();

        if (ids.Length == 0) return new Dictionary<string, UserRefDto>();

        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/v1/users/batch", new { ids }, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("UserService profile lookup returned {StatusCode}.", response.StatusCode);
                return new Dictionary<string, UserRefDto>();
            }

            var payload = await response.Content.ReadFromJsonAsync<BatchResponse>(cancellationToken: ct);
            if (payload?.Data is null) return new Dictionary<string, UserRefDto>();

            return payload.Data.Values
                .ToDictionary(user => user.Id, user => new UserRefDto(user.Id, user.Username, user.DisplayName, user.AvatarUrl));
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "UserService profile lookup failed.");
            return new Dictionary<string, UserRefDto>();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "UserService returned an invalid profile response.");
            return new Dictionary<string, UserRefDto>();
        }
    }

    private sealed record BatchResponse(IReadOnlyDictionary<string, PublicUser> Data);
    private sealed record PublicUser(string Id, string Username, string DisplayName, string? AvatarUrl);
}

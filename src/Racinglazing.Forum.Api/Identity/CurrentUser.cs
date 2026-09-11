using System.Security.Claims;
using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Api.Identity;

/// <summary>
/// Resolves the current user from the request. In production this comes from
/// validated JWT claims (populated by the gateway/IdP). For local development —
/// while UserService/auth don't yet exist — it can optionally fall back to
/// X-User-Id / X-User-Roles headers, gated by configuration.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly string? _userId;
    private readonly IReadOnlyCollection<string> _roles;

    public CurrentUser(IHttpContextAccessor accessor, IConfiguration configuration)
    {
        var http = accessor.HttpContext;
        var principal = http?.User;

        if (principal?.Identity?.IsAuthenticated == true)
        {
            _userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub");
            _roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(principal.FindAll("role").Select(c => c.Value))
                .Select(Normalize).Distinct().ToArray();
        }
        else if (configuration.GetValue("ForumAuth:AllowDevHeaders", false) && http is not null)
        {
            _userId = Header(http, "X-User-Id");
            var roles = Header(http, "X-User-Roles");
            _roles = string.IsNullOrWhiteSpace(roles)
                ? []
                : roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Normalize).ToArray();
        }
        else
        {
            _roles = [];
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);
    public string? UserId => _userId;
    public IReadOnlyCollection<string> Roles => _roles;
    public bool IsModerator => _roles.Contains("moderator") || _roles.Contains("admin");

    public string RequireUserId() => _userId
        ?? throw new UnauthorizedException("UNAUTHENTICATED", "Authentication is required for this operation.");

    private static string? Header(HttpContext http, string name)
        => http.Request.Headers.TryGetValue(name, out var v) ? v.ToString() : null;

    private static string Normalize(string role) => role.Trim().ToLowerInvariant();
}

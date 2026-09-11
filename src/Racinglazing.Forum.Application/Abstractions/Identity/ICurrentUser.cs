namespace Racinglazing.Forum.Application.Abstractions.Identity;

/// <summary>
/// Ambient authenticated-user context, resolved from the request's identity/
/// claims. ForumService authorizes operations against this — it never trusts an
/// authorId supplied in a request body.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The authenticated user's id (UserService reference), or null.</summary>
    string? UserId { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsModerator { get; }

    /// <summary>Returns the user id or throws if the request is unauthenticated.</summary>
    string RequireUserId();
}

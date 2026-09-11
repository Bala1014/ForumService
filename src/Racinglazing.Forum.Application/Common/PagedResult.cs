namespace Racinglazing.Forum.Application.Common;

/// <summary>
/// A cursor-paginated slice of results. Maps directly onto the contract's
/// { data: [], pagination: { nextCursor, hasMore } } envelope.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore)
{
    public static PagedResult<T> Empty { get; } = new([], null, false);
}

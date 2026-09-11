namespace Racinglazing.Forum.Application.Common;

/// <summary>Normalised paging inputs shared by all list endpoints.</summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int PageSize { get; }
    public string? Cursor { get; }

    public PageRequest(int? pageSize, string? cursor)
    {
        PageSize = pageSize is null or <= 0
            ? DefaultPageSize
            : Math.Min(pageSize.Value, MaxPageSize);
        Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor;
    }
}

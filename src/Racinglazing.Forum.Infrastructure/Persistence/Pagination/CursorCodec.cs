using System.Text;
using System.Text.Json;

namespace Racinglazing.Forum.Infrastructure.Persistence.Pagination;

/// <summary>
/// Encodes/decodes opaque, URL-safe pagination cursors. A cursor carries the
/// ordering key(s) of the last row on a page so the next page can be fetched
/// via a keyset predicate (WHERE key &lt; cursor) rather than OFFSET — which
/// keeps deep pages O(1) instead of O(n).
/// </summary>
public static class CursorCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Encode<T>(T payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, Options);
        return Convert.ToBase64String(json)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static T? Decode<T>(string? cursor) where T : class
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded);
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }
        catch
        {
            // A malformed cursor is treated as "start from the beginning".
            return null;
        }
    }
}

/// <summary>Cursor payload for thread feeds (pinned-first + a numeric sort key).</summary>
public sealed record ThreadCursor(int Pinned, string Key, Guid Id);

/// <summary>Cursor payload for comment listings.</summary>
public sealed record CommentCursor(string Key, Guid Id);

/// <summary>Cursor payload for simple entity listings ordered by creation.</summary>
public sealed record CreatedCursor(long Ticks, Guid Id);

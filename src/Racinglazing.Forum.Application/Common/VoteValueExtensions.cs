using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Common;

public static class VoteValueExtensions
{
    public const string Up = "up";
    public const string Down = "down";

    public static string? ToApiString(this VoteValue? value) => value switch
    {
        VoteValue.Up => Up,
        VoteValue.Down => Down,
        _ => null
    };

    public static string ToApiString(this VoteValue value) => value switch
    {
        VoteValue.Up => Up,
        _ => Down
    };

    public static bool TryParse(string? raw, out VoteValue value)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case Up: value = VoteValue.Up; return true;
            case Down: value = VoteValue.Down; return true;
            default: value = default; return false;
        }
    }
}

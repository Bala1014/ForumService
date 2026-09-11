namespace Racinglazing.Forum.Domain.Enums;

/// <summary>
/// Sort orders supported when listing comments. Controversial is declared for
/// later; Top/New/Old are wired for the MVP.
/// </summary>
public enum CommentSort
{
    Top = 0,
    New = 1,
    Old = 2,
    Controversial = 3
}

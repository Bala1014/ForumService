namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// Value object for a comment's body. Currently just text, but modelled as an
/// owned type so that media references (blob storage metadata) can be added
/// later without changing how content is stored or exposed.
/// </summary>
public sealed record CommentContent(string Text)
{
    public static readonly CommentContent Empty = new(string.Empty);
}

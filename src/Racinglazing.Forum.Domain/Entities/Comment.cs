using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// The core discussion unit. A comment is either:
///  - the thread's root comment (body): IsRoot = true, ParentCommentId = null;
///  - a top-level discussion comment: IsRoot = false, ParentCommentId = null;
///  - a nested reply: ParentCommentId = &lt;parent&gt;.
///
/// The IsRoot flag is what distinguishes the thread body from top-level
/// comments (both have a null parent) so the comments listing can exclude it.
/// </summary>
public class Comment : Entity, IVotable
{
    public Guid ThreadId { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    /// <summary>External UserService reference (see Thread.AuthorId).</summary>
    public string AuthorId { get; private set; } = string.Empty;

    /// <summary>Body text. Modelled as an owned value object so media
    /// references can be added later without touching the comment table shape
    /// callers depend on.</summary>
    public CommentContent Content { get; private set; } = CommentContent.Empty;

    public int Depth { get; private set; }
    public bool IsRoot { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public int Score { get; private set; }
    public int UpvoteCount { get; private set; }
    public int DownvoteCount { get; private set; }
    public int ReplyCount { get; private set; }

    public double HotScore { get; private set; }
    public double ControversyScore { get; private set; }

    public bool IsEdited { get; private set; }
    public bool IsDeleted { get; private set; }

    private Comment() { }

    private Comment(Guid threadId, Guid? parentCommentId, string authorId,
        CommentContent content, int depth, bool isRoot)
    {
        ThreadId = threadId;
        ParentCommentId = parentCommentId;
        AuthorId = authorId;
        Content = content;
        Depth = depth;
        IsRoot = isRoot;
        HotScore = Ranking.Hot(0, 0, CreatedAt);
    }

    public static Comment CreateRoot(Guid threadId, string authorId, CommentContent content)
        => new(threadId, null, authorId, content, depth: 0, isRoot: true);

    public static Comment CreateTopLevel(Guid threadId, string authorId, CommentContent content)
        => new(threadId, null, authorId, content, depth: 0, isRoot: false);

    public static Comment CreateReply(Guid threadId, Comment parent, string authorId, CommentContent content)
        => new(threadId, parent.Id, authorId, content, depth: parent.Depth + 1, isRoot: false);

    public void ApplyVoteTotals(int upvotes, int downvotes)
    {
        UpvoteCount = upvotes;
        DownvoteCount = downvotes;
        Score = upvotes - downvotes;
        HotScore = Ranking.Hot(upvotes, downvotes, CreatedAt);
        ControversyScore = Ranking.Controversy(upvotes, downvotes);
    }

    public void RegisterReplyAdded() => ReplyCount++;
    public void RegisterReplyRemoved() => ReplyCount = Math.Max(0, ReplyCount - 1);

    public void Edit(CommentContent content, DateTimeOffset at)
    {
        Content = content;
        IsEdited = true;
        UpdatedAt = at;
    }

    public void SoftDelete(DateTimeOffset at)
    {
        IsDeleted = true;
        DeletedAt = at;
        // Preserve tree structure; blank the body so it stops being readable.
        Content = CommentContent.Empty;
    }
}

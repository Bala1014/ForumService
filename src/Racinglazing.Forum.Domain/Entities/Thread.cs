using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// A discussion topic within a forum. A thread owns exactly one root comment
/// (its body); discussion comments hang off the thread separately.
/// </summary>
public class Thread : Entity, IVotable
{
    public Guid ForumId { get; private set; }
    public Forum? Forum { get; private set; }

    /// <summary>External UserService reference. Stored as text — ForumService
    /// never owns user identity, so we do not couple to their id format.</summary>
    public string AuthorId { get; private set; } = string.Empty;

    /// <summary>Optional external RaceService reference. RaceService is the
    /// source of truth; we only keep the id.</summary>
    public string? RaceId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    /// <summary>The root comment id (thread body). Nullable only transiently
    /// during creation; always set once persisted.</summary>
    public Guid? RootCommentId { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; } = DateTimeOffset.UtcNow;

    // Denormalised vote / activity counters.
    public int Score { get; private set; }
    public int UpvoteCount { get; private set; }
    public int DownvoteCount { get; private set; }
    public int CommentCount { get; private set; }
    public long ViewCount { get; private set; }

    /// <summary>Precomputed hot ranking, updated on every vote so feeds sort
    /// via a plain indexed ORDER BY.</summary>
    public double HotScore { get; private set; }

    // Moderation / lifecycle flags. We deliberately model these as booleans
    // (the API surface) rather than a single Status enum, to avoid two sources
    // of truth for the same state.
    public bool IsPinned { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? LockedAt { get; private set; }
    public string? LockReason { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private readonly List<ThreadTag> _threadTags = new();
    public IReadOnlyCollection<ThreadTag> ThreadTags => _threadTags.AsReadOnly();

    private Thread() { }

    public Thread(Guid forumId, string authorId, string title, string slug, string? raceId)
    {
        ForumId = forumId;
        AuthorId = authorId;
        Title = title;
        Slug = slug;
        RaceId = raceId;
        LastActivityAt = CreatedAt;
        HotScore = Ranking.Hot(0, 0, CreatedAt);
    }

    public void AttachRootComment(Guid rootCommentId) => RootCommentId = rootCommentId;

    public void AddTag(Guid tagId)
    {
        if (_threadTags.Any(tt => tt.TagId == tagId)) return;
        _threadTags.Add(new ThreadTag(Id, tagId));
    }

    public void ApplyVoteTotals(int upvotes, int downvotes)
    {
        UpvoteCount = upvotes;
        DownvoteCount = downvotes;
        Score = upvotes - downvotes;
        HotScore = Ranking.Hot(upvotes, downvotes, CreatedAt);
    }

    public void RegisterCommentAdded(DateTimeOffset at)
    {
        CommentCount++;
        LastActivityAt = at;
    }

    public void RegisterCommentRemoved() => CommentCount = Math.Max(0, CommentCount - 1);

    public void IncrementView() => ViewCount++;

    public void Edit(DateTimeOffset at) => UpdatedAt = at;

    // --- Moderation operations ---
    public void Pin() => IsPinned = true;
    public void Unpin() => IsPinned = false;

    public void Lock(string? reason, DateTimeOffset at)
    {
        IsLocked = true;
        LockReason = reason;
        LockedAt = at;
    }

    public void Unlock()
    {
        IsLocked = false;
        LockReason = null;
        LockedAt = null;
    }

    public void SoftDelete(DateTimeOffset at)
    {
        IsDeleted = true;
        DeletedAt = at;
    }
}

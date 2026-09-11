using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetActiveOrderedAsync(CancellationToken ct = default);
    Task<Category?> GetByIdWithForumsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tracked fetch for counter maintenance (no includes).</summary>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IForumRepository
{
    Task<DomainForum?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<DomainForum>> ListAsync(Guid? categoryId, PageRequest page, CancellationToken ct = default);
}

public interface IThreadRepository
{
    void Add(DomainThread thread);

    /// <summary>Loads a thread with its tags (for detail/summary mapping).</summary>
    Task<DomainThread?> GetByIdAsync(Guid id, bool includeTags = false, bool includeForum = false, CancellationToken ct = default);

    Task<bool> SlugExistsInForumAsync(Guid forumId, string slug, CancellationToken ct = default);

    Task<PagedResult<DomainThread>> ListByForumAsync(Guid forumId, ThreadSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<DomainThread>> ListByTagAsync(Guid tagId, ThreadSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<DomainThread>> ListByRaceAsync(string raceId, ThreadSort sort, PageRequest page, CancellationToken ct = default);
}

public interface ICommentRepository
{
    void Add(Comment comment);
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Comment>> ListTopLevelAsync(Guid threadId, CommentSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<Comment>> ListRepliesAsync(Guid parentCommentId, CommentSort sort, PageRequest page, CancellationToken ct = default);
}

public interface IVoteRepository
{
    void Add(Vote vote);
    void Remove(Vote vote);
    Task<Vote?> GetAsync(string userId, VoteTargetType targetType, Guid targetId, CancellationToken ct = default);

    /// <summary>Current-user votes for a set of targets, for userVote enrichment.</summary>
    Task<IReadOnlyDictionary<Guid, VoteValue>> GetUserVotesAsync(
        string userId, VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, CancellationToken ct = default);
}

public interface ITagRepository
{
    void Add(Tag tag);
    Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> GetBySlugsAsync(IReadOnlyCollection<string> slugs, CancellationToken ct = default);
    Task<PagedResult<Tag>> SearchAsync(string? search, PageRequest page, CancellationToken ct = default);
}

public interface IReportRepository
{
    void Add(Report report);
    Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Report>> ListAsync(ReportStatus? status, VoteTargetType? targetType, PageRequest page, CancellationToken ct = default);
}

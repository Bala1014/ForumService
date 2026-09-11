using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Application.Features.Threads;

public interface IThreadService
{
    Task<CreateThreadResultDto> CreateAsync(Guid forumId, CreateThreadRequest request, CancellationToken ct = default);
    Task<ThreadDetailDto> GetAsync(Guid threadId, CancellationToken ct = default);
    Task<PagedResult<ThreadSummaryDto>> ListByForumAsync(Guid forumId, ThreadSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<ThreadSummaryDto>> ListByTagAsync(string tagSlug, ThreadSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<ThreadSummaryDto>> ListByRaceAsync(string raceId, ThreadSort sort, PageRequest page, CancellationToken ct = default);
    Task<ThreadDeleteResultDto> DeleteAsync(Guid threadId, CancellationToken ct = default);

    // Moderation
    Task<ThreadModerationStateDto> SetPinnedAsync(Guid threadId, bool pinned, CancellationToken ct = default);
    Task<ThreadLockStateDto> SetLockedAsync(Guid threadId, bool locked, string? reason, CancellationToken ct = default);
}

public sealed class ThreadService(
    IThreadRepository threads,
    IForumRepository forums,
    ICategoryRepository categories,
    ICommentRepository comments,
    ITagRepository tags,
    IVoteRepository votes,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IRaceServiceClient raceService,
    ISlugGenerator slugs,
    IDateTimeProvider clock,
    ResponseEnricher enricher) : IThreadService
{
    public async Task<CreateThreadResultDto> CreateAsync(Guid forumId, CreateThreadRequest request, CancellationToken ct = default)
    {
        var authorId = currentUser.RequireUserId();

        var forum = await forums.GetByIdAsync(forumId, ct)
            ?? throw new NotFoundException("FORUM_NOT_FOUND", "The requested forum was not found.");

        if (!string.IsNullOrWhiteSpace(request.RaceId) &&
            !await raceService.RaceExistsAsync(request.RaceId, ct))
        {
            throw new NotFoundException("RACE_NOT_FOUND", "The referenced race was not found.");
        }

        var slug = await GenerateUniqueSlugAsync(forumId, request.Title, ct);
        var now = clock.UtcNow;

        var thread = new DomainThread(forumId, authorId, request.Title.Trim(), slug,
            NormalizeRaceId(request.RaceId));

        var rootComment = Comment.CreateRoot(thread.Id, authorId, new CommentContent(request.Content.Text));
        thread.AttachRootComment(rootComment.Id);

        await AttachTagsAsync(thread, request.Tags, ct);

        forum.RegisterThreadAdded(now);
        forum.RegisterPostAdded(now); // root comment counts as a post

        var category = await categories.GetByIdAsync(forum.CategoryId, ct);
        category?.RegisterThreadAdded();

        threads.Add(thread);
        comments.Add(rootComment);

        await uow.SaveChangesAsync(ct);

        return new CreateThreadResultDto(
            thread.Id.ToString(), forumId.ToString(), authorId, thread.Title, thread.Slug,
            rootComment.Id.ToString(), thread.CreatedAt);
    }

    public async Task<ThreadDetailDto> GetAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await threads.GetByIdAsync(threadId, includeTags: true, includeForum: true, ct);
        if (thread is null || thread.IsDeleted)
            throw new NotFoundException("THREAD_NOT_FOUND", "The requested thread was not found.");

        thread.IncrementView();
        await uow.SaveChangesAsync(ct);

        var author = await enricher.ResolveSingleAuthorAsync(thread.AuthorId, ct);
        var tagDtos = thread.ThreadTags.Where(t => t.Tag is not null)
            .Select(t => ContractMapper.ToTagDto(t.Tag!)).ToList();

        CommentDto? rootDto = null;
        if (thread.RootCommentId is { } rootId)
        {
            var root = await comments.GetByIdAsync(rootId, ct);
            if (root is not null) rootDto = await enricher.EnrichCommentAsync(root, ct);
        }

        var threadVote = await GetUserVoteAsync(VoteTargetType.Thread, thread.Id, ct);
        return ContractMapper.ToDetail(thread, thread.Forum!, author, tagDtos, threadVote, rootDto);
    }

    public async Task<PagedResult<ThreadSummaryDto>> ListByForumAsync(Guid forumId, ThreadSort sort, PageRequest page, CancellationToken ct = default)
    {
        var result = await threads.ListByForumAsync(forumId, sort, page, ct);
        return await ToSummaryPageAsync(result, ct);
    }

    public async Task<PagedResult<ThreadSummaryDto>> ListByTagAsync(string tagSlug, ThreadSort sort, PageRequest page, CancellationToken ct = default)
    {
        var tag = await tags.GetBySlugAsync(tagSlug, ct)
            ?? throw new NotFoundException("TAG_NOT_FOUND", "The requested tag was not found.");
        var result = await threads.ListByTagAsync(tag.Id, sort, page, ct);
        return await ToSummaryPageAsync(result, ct);
    }

    public async Task<PagedResult<ThreadSummaryDto>> ListByRaceAsync(string raceId, ThreadSort sort, PageRequest page, CancellationToken ct = default)
    {
        var result = await threads.ListByRaceAsync(raceId, sort, page, ct);
        return await ToSummaryPageAsync(result, ct);
    }

    public async Task<ThreadDeleteResultDto> DeleteAsync(Guid threadId, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var thread = await threads.GetByIdAsync(threadId, ct: ct)
            ?? throw new NotFoundException("THREAD_NOT_FOUND", "The requested thread was not found.");

        if (thread.AuthorId != userId && !currentUser.IsModerator)
            throw new ForbiddenException("FORBIDDEN", "You are not allowed to delete this thread.");

        if (thread.IsDeleted)
            return new ThreadDeleteResultDto(thread.Id.ToString(), true, thread.DeletedAt);

        var now = clock.UtcNow;
        thread.SoftDelete(now);

        var forum = await forums.GetByIdAsync(thread.ForumId, ct);
        forum?.RegisterThreadRemoved();
        if (forum is not null)
        {
            var category = await categories.GetByIdAsync(forum.CategoryId, ct);
            category?.RegisterThreadRemoved();
        }

        await uow.SaveChangesAsync(ct);
        return new ThreadDeleteResultDto(thread.Id.ToString(), true, now);
    }

    public async Task<ThreadModerationStateDto> SetPinnedAsync(Guid threadId, bool pinned, CancellationToken ct = default)
    {
        RequireModerator();
        var thread = await threads.GetByIdAsync(threadId, ct: ct)
            ?? throw new NotFoundException("THREAD_NOT_FOUND", "The requested thread was not found.");

        if (pinned) thread.Pin(); else thread.Unpin();
        await uow.SaveChangesAsync(ct);
        return new ThreadModerationStateDto(thread.Id.ToString(), thread.IsPinned);
    }

    public async Task<ThreadLockStateDto> SetLockedAsync(Guid threadId, bool locked, string? reason, CancellationToken ct = default)
    {
        RequireModerator();
        var thread = await threads.GetByIdAsync(threadId, ct: ct)
            ?? throw new NotFoundException("THREAD_NOT_FOUND", "The requested thread was not found.");

        if (locked) thread.Lock(reason, clock.UtcNow); else thread.Unlock();
        await uow.SaveChangesAsync(ct);
        return new ThreadLockStateDto(thread.Id.ToString(), thread.IsLocked, thread.LockedAt);
    }

    // --- helpers ---

    private async Task<PagedResult<ThreadSummaryDto>> ToSummaryPageAsync(PagedResult<DomainThread> result, CancellationToken ct)
    {
        var items = await enricher.EnrichThreadsAsync(result.Items, ct);
        return new PagedResult<ThreadSummaryDto>(items, result.NextCursor, result.HasMore);
    }

    private async Task AttachTagsAsync(DomainThread thread, IReadOnlyList<string>? requested, CancellationToken ct)
    {
        if (requested is null || requested.Count == 0) return;

        var normalized = requested
            .Select(t => (Raw: t.Trim(), Slug: slugs.Generate(t)))
            .Where(t => t.Slug.Length > 0)
            .GroupBy(t => t.Slug)
            .Select(g => g.First())
            .ToList();

        var existing = (await tags.GetBySlugsAsync(normalized.Select(n => n.Slug).ToList(), ct))
            .ToDictionary(t => t.Slug);

        foreach (var (raw, slug) in normalized)
        {
            if (!existing.TryGetValue(slug, out var tag))
            {
                tag = new Tag(raw.Length > 0 ? raw : slug, slug);
                tags.Add(tag);
                existing[slug] = tag;
            }
            tag.RegisterThreadAdded();
            thread.AddTag(tag.Id);
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid forumId, string title, CancellationToken ct)
    {
        var baseSlug = slugs.Generate(title);
        if (baseSlug.Length == 0) baseSlug = "thread";
        if (!await threads.SlugExistsInForumAsync(forumId, baseSlug, ct)) return baseSlug;

        // Collisions are rare; append a short suffix from a fresh UUID.
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{baseSlug}-{suffix}";
    }

    private async Task<VoteValue?> GetUserVoteAsync(VoteTargetType type, Guid targetId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) return null;
        var vote = await votes.GetAsync(currentUser.UserId!, type, targetId, ct);
        return vote?.Value;
    }

    private static string? NormalizeRaceId(string? raceId)
        => string.IsNullOrWhiteSpace(raceId) ? null : raceId.Trim();

    private void RequireModerator()
    {
        currentUser.RequireUserId();
        if (!currentUser.IsModerator)
            throw new ForbiddenException("FORBIDDEN", "This operation requires moderator privileges.");
    }
}

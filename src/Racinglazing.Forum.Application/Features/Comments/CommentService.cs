using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Features.Comments;

public interface ICommentService
{
    Task<PagedResult<CommentDto>> ListTopLevelAsync(Guid threadId, CommentSort sort, PageRequest page, CancellationToken ct = default);
    Task<PagedResult<CommentDto>> ListRepliesAsync(Guid commentId, CommentSort sort, PageRequest page, CancellationToken ct = default);
    Task<CreateCommentResultDto> CreateTopLevelAsync(Guid threadId, CreateCommentRequest request, CancellationToken ct = default);
    Task<CreateCommentResultDto> CreateReplyAsync(Guid parentCommentId, CreateCommentRequest request, CancellationToken ct = default);
}

public sealed class CommentService(
    ICommentRepository comments,
    IThreadRepository threads,
    IForumRepository forums,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ResponseEnricher enricher) : ICommentService
{
    public async Task<PagedResult<CommentDto>> ListTopLevelAsync(Guid threadId, CommentSort sort, PageRequest page, CancellationToken ct = default)
    {
        var result = await comments.ListTopLevelAsync(threadId, sort, page, ct);
        return await ToPageAsync(result, ct);
    }

    public async Task<PagedResult<CommentDto>> ListRepliesAsync(Guid commentId, CommentSort sort, PageRequest page, CancellationToken ct = default)
    {
        var result = await comments.ListRepliesAsync(commentId, sort, page, ct);
        return await ToPageAsync(result, ct);
    }

    public async Task<CreateCommentResultDto> CreateTopLevelAsync(Guid threadId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var authorId = currentUser.RequireUserId();
        var thread = await LoadPostableThreadAsync(threadId, ct);

        var comment = Comment.CreateTopLevel(thread.Id, authorId, new CommentContent(request.Content.Text));
        comments.Add(comment);

        await RegisterPostAsync(thread, ct);
        await uow.SaveChangesAsync(ct);
        return Map(comment, authorId);
    }

    public async Task<CreateCommentResultDto> CreateReplyAsync(Guid parentCommentId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var authorId = currentUser.RequireUserId();

        var parent = await comments.GetByIdAsync(parentCommentId, ct);
        if (parent is null || parent.IsDeleted)
            throw new NotFoundException("COMMENT_NOT_FOUND", "The parent comment was not found.");

        var thread = await LoadPostableThreadAsync(parent.ThreadId, ct);

        var reply = Comment.CreateReply(thread.Id, parent, authorId, new CommentContent(request.Content.Text));
        comments.Add(reply);
        parent.RegisterReplyAdded();

        await RegisterPostAsync(thread, ct);
        await uow.SaveChangesAsync(ct);
        return Map(reply, authorId);
    }

    private async Task<Domain.Entities.Thread> LoadPostableThreadAsync(Guid threadId, CancellationToken ct)
    {
        var thread = await threads.GetByIdAsync(threadId, ct: ct);
        if (thread is null || thread.IsDeleted)
            throw new NotFoundException("THREAD_NOT_FOUND", "The requested thread was not found.");
        if (thread.IsLocked)
            throw new ConflictException("THREAD_LOCKED", "This thread is locked and cannot receive new comments.");
        return thread;
    }

    private async Task RegisterPostAsync(Domain.Entities.Thread thread, CancellationToken ct)
    {
        var now = clock.UtcNow;
        thread.RegisterCommentAdded(now);
        // Keep the forum's activity/post counters current for fast listings.
        var forum = await forums.GetByIdAsync(thread.ForumId, ct);
        forum?.RegisterPostAdded(now);
    }

    private async Task<PagedResult<CommentDto>> ToPageAsync(PagedResult<Comment> result, CancellationToken ct)
    {
        var items = await enricher.EnrichCommentsAsync(result.Items, ct);
        return new PagedResult<CommentDto>(items, result.NextCursor, result.HasMore);
    }

    private static CreateCommentResultDto Map(Comment c, string authorId) => new(
        c.Id.ToString(), c.ThreadId.ToString(), c.ParentCommentId?.ToString(), authorId,
        new ContentDto(c.Content.Text), c.Score, c.UpvoteCount, c.DownvoteCount, c.ReplyCount,
        c.Depth, c.CreatedAt, c.IsEdited, c.IsDeleted);
}

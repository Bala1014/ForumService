using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Application.Common;

/// <summary>
/// Pure entity → DTO projections. Author enrichment and userVote are passed in
/// (resolved once, in bulk, by the service) rather than fetched here, so the
/// mapper stays free of I/O.
/// </summary>
public static class ContractMapper
{
    public static UserRefDto UnknownUser(string userId) =>
        new(userId, userId, userId, null);

    public static TagDto ToTagDto(Tag tag) => new(tag.Id.ToString(), tag.Name, tag.Slug);

    public static ThreadSummaryDto ToSummary(DomainThread t, UserRefDto author,
        IReadOnlyList<TagDto> tags, VoteValue? userVote) =>
        new(
            t.Id.ToString(),
            t.ForumId.ToString(),
            author,
            t.Title,
            t.Slug,
            t.Score,
            t.UpvoteCount,
            t.DownvoteCount,
            t.CommentCount,
            t.ViewCount,
            t.CreatedAt,
            t.LastActivityAt,
            t.IsPinned,
            t.IsLocked,
            tags,
            t.RaceId,
            userVote.ToApiString());

    public static ThreadDetailDto ToDetail(DomainThread t, DomainForum forum, UserRefDto author,
        IReadOnlyList<TagDto> tags, VoteValue? threadVote, CommentDto? rootComment) =>
        new(
            t.Id.ToString(),
            new ThreadForumRefDto(forum.Id.ToString(), forum.Name, forum.Slug),
            author,
            t.Title,
            t.Slug,
            t.Score,
            t.UpvoteCount,
            t.DownvoteCount,
            t.CommentCount,
            t.ViewCount,
            t.CreatedAt,
            t.UpdatedAt,
            t.LastActivityAt,
            t.IsPinned,
            t.IsLocked,
            t.IsDeleted,
            threadVote.ToApiString(),
            tags,
            t.RaceId is null ? null : new RaceRefDto(t.RaceId),
            rootComment);

    public static CommentDto ToCommentDto(Comment c, UserRefDto author, VoteValue? userVote) =>
        new(
            c.Id.ToString(),
            c.ThreadId.ToString(),
            c.ParentCommentId?.ToString(),
            author,
            new ContentDto(c.Content.Text),
            c.Score,
            c.UpvoteCount,
            c.DownvoteCount,
            c.ReplyCount,
            c.Depth,
            c.CreatedAt,
            c.UpdatedAt,
            c.IsEdited,
            c.IsDeleted,
            userVote.ToApiString());
}

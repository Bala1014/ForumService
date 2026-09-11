using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Application.Common;

/// <summary>
/// Turns entity collections into response DTOs, resolving author profiles and
/// the current user's votes in <b>bulk</b> (one call each) to avoid N+1 lookups.
/// </summary>
public sealed class ResponseEnricher(
    IUserProfileProvider userProfiles,
    IVoteRepository votes,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<ThreadSummaryDto>> EnrichThreadsAsync(
        IReadOnlyList<DomainThread> threads, CancellationToken ct)
    {
        if (threads.Count == 0) return [];

        var authors = await ResolveAuthorsAsync(threads.Select(t => t.AuthorId), ct);
        var userVotes = await ResolveUserVotesAsync(VoteTargetType.Thread, threads.Select(t => t.Id), ct);

        return threads.Select(t =>
        {
            var tags = t.ThreadTags
                .Where(tt => tt.Tag is not null)
                .Select(tt => ContractMapper.ToTagDto(tt.Tag!))
                .ToList();
            userVotes.TryGetValue(t.Id, out var vote);
            return ContractMapper.ToSummary(t, ResolveAuthor(authors, t.AuthorId), tags,
                userVotes.ContainsKey(t.Id) ? vote : null);
        }).ToList();
    }

    public async Task<IReadOnlyList<CommentDto>> EnrichCommentsAsync(
        IReadOnlyList<Comment> comments, CancellationToken ct)
    {
        if (comments.Count == 0) return [];

        var authors = await ResolveAuthorsAsync(comments.Select(c => c.AuthorId), ct);
        var userVotes = await ResolveUserVotesAsync(VoteTargetType.Comment, comments.Select(c => c.Id), ct);

        return comments.Select(c =>
        {
            userVotes.TryGetValue(c.Id, out var vote);
            return ContractMapper.ToCommentDto(c, ResolveAuthor(authors, c.AuthorId),
                userVotes.ContainsKey(c.Id) ? vote : null);
        }).ToList();
    }

    public async Task<CommentDto> EnrichCommentAsync(Comment comment, CancellationToken ct)
        => (await EnrichCommentsAsync([comment], ct))[0];

    public async Task<UserRefDto> ResolveSingleAuthorAsync(string authorId, CancellationToken ct)
    {
        var authors = await ResolveAuthorsAsync([authorId], ct);
        return ResolveAuthor(authors, authorId);
    }

    private async Task<IReadOnlyDictionary<string, UserRefDto>> ResolveAuthorsAsync(
        IEnumerable<string> authorIds, CancellationToken ct)
    {
        var ids = authorIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        return ids.Count == 0
            ? new Dictionary<string, UserRefDto>()
            : await userProfiles.GetProfilesAsync(ids, ct);
    }

    private async Task<IReadOnlyDictionary<Guid, VoteValue>> ResolveUserVotesAsync(
        VoteTargetType targetType, IEnumerable<Guid> targetIds, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) return new Dictionary<Guid, VoteValue>();
        var ids = targetIds.Distinct().ToList();
        return await votes.GetUserVotesAsync(currentUser.UserId!, targetType, ids, ct);
    }

    private static UserRefDto ResolveAuthor(IReadOnlyDictionary<string, UserRefDto> authors, string authorId)
        => authors.TryGetValue(authorId, out var user) ? user : ContractMapper.UnknownUser(authorId);
}

using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Application.Features.Forums;

public interface IForumService
{
    Task<PagedResult<ForumDto>> ListAsync(Guid? categoryId, PageRequest page, CancellationToken ct = default);
    Task<ForumDto> GetByIdAsync(Guid forumId, CancellationToken ct = default);
}

public sealed class ForumService(IForumRepository forums) : IForumService
{
    public async Task<PagedResult<ForumDto>> ListAsync(Guid? categoryId, PageRequest page, CancellationToken ct = default)
    {
        var result = await forums.ListAsync(categoryId, page, ct);
        return new PagedResult<ForumDto>(result.Items.Select(Map).ToList(), result.NextCursor, result.HasMore);
    }

    public async Task<ForumDto> GetByIdAsync(Guid forumId, CancellationToken ct = default)
    {
        var forum = await forums.GetByIdAsync(forumId, ct)
            ?? throw new NotFoundException("FORUM_NOT_FOUND", "The requested forum was not found.");
        return Map(forum);
    }

    private static ForumDto Map(DomainForum f) => new(
        f.Id.ToString(), f.CategoryId.ToString(), f.Name, f.Slug, f.Description,
        f.ThreadCount, f.PostCount, f.LastActivityAt);
}

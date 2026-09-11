using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;

namespace Racinglazing.Forum.Application.Features.Tags;

public interface ITagService
{
    Task<PagedResult<TagSummaryDto>> SearchAsync(string? search, PageRequest page, CancellationToken ct = default);
}

public sealed class TagService(ITagRepository tags) : ITagService
{
    public async Task<PagedResult<TagSummaryDto>> SearchAsync(string? search, PageRequest page, CancellationToken ct = default)
    {
        var result = await tags.SearchAsync(search, page, ct);
        var items = result.Items
            .Select(t => new TagSummaryDto(t.Id.ToString(), t.Name, t.Slug, t.ThreadCount))
            .ToList();
        return new PagedResult<TagSummaryDto>(items, result.NextCursor, result.HasMore);
    }
}

using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Application.Features.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategorySummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDetailDto> GetByIdAsync(Guid categoryId, CancellationToken ct = default);
}

public sealed class CategoryService(ICategoryRepository categories) : ICategoryService
{
    public async Task<IReadOnlyList<CategorySummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await categories.GetActiveOrderedAsync(ct);
        return items.Select(c => new CategorySummaryDto(
            c.Id.ToString(), c.Name, c.Slug, c.Description, c.DisplayOrder,
            c.ForumCount, c.ThreadCount)).ToList();
    }

    public async Task<CategoryDetailDto> GetByIdAsync(Guid categoryId, CancellationToken ct = default)
    {
        var category = await categories.GetByIdWithForumsAsync(categoryId, ct)
            ?? throw new NotFoundException("CATEGORY_NOT_FOUND", "The requested category was not found.");

        var forums = category.Forums
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new CategoryForumDto(
                f.Id.ToString(), f.Name, f.Slug, f.Description, f.ThreadCount, f.PostCount))
            .ToList();

        return new CategoryDetailDto(
            category.Id.ToString(), category.Name, category.Slug, category.Description, forums);
    }
}

using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Domain.Entities;

namespace Racinglazing.Forum.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ForumDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetActiveOrderedAsync(CancellationToken ct = default)
        => await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Category?> GetByIdWithForumsAsync(Guid id, CancellationToken ct = default)
        => await db.Categories.AsNoTracking()
            .Include(c => c.Forums.Where(f => f.IsActive))
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
}

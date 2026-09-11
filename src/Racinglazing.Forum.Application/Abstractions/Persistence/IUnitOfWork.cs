namespace Racinglazing.Forum.Application.Abstractions.Persistence;

/// <summary>
/// Commits all pending changes tracked across repositories as a single atomic
/// unit. Repositories stage work; the use-case service decides when to commit.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

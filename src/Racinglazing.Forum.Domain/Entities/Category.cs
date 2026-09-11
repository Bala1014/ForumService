using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// Highest-level grouping (e.g. "Formula 1"). Categories are administrative
/// configuration data; for the MVP users only read them.
/// </summary>
public class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Denormalised counters kept current on write so category/forum listings
    // never pay for a cross-table aggregate at read time.
    public int ForumCount { get; private set; }
    public long ThreadCount { get; private set; }

    private readonly List<Forum> _forums = new();
    public IReadOnlyCollection<Forum> Forums => _forums.AsReadOnly();

    private Category() { }

    public Category(string name, string slug, string? description, int displayOrder)
    {
        Name = name;
        Slug = slug;
        Description = description;
        DisplayOrder = displayOrder;
    }

    public void RegisterForumAdded() => ForumCount++;

    public void RegisterThreadAdded() => ThreadCount++;

    public void RegisterThreadRemoved() => ThreadCount = Math.Max(0, ThreadCount - 1);
}

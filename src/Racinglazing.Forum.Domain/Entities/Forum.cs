using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Domain.Entities;

/// <summary>
/// A forum belongs to a category (e.g. "Race Discussion" under "Formula 1").
/// </summary>
public class Forum : Entity
{
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Denormalised counters for fast listings.
    public long ThreadCount { get; private set; }
    public long PostCount { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }

    private Forum() { }

    public Forum(Guid categoryId, string name, string slug, string? description, int displayOrder)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        Description = description;
        DisplayOrder = displayOrder;
    }

    public void RegisterThreadAdded(DateTimeOffset at)
    {
        ThreadCount++;
        LastActivityAt = at;
    }

    public void RegisterThreadRemoved() => ThreadCount = Math.Max(0, ThreadCount - 1);

    public void RegisterPostAdded(DateTimeOffset at)
    {
        PostCount++;
        LastActivityAt = at;
    }

    public void TouchActivity(DateTimeOffset at) => LastActivityAt = at;
}

using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Domain.Entities;

public class Tag : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public long ThreadCount { get; private set; }

    private Tag() { }

    public Tag(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }

    public void RegisterThreadAdded() => ThreadCount++;
    public void RegisterThreadRemoved() => ThreadCount = Math.Max(0, ThreadCount - 1);
}

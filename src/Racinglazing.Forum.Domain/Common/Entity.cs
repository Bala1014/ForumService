namespace Racinglazing.Forum.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Ids are UUID v7 (time-ordered) so that
/// primary keys are globally unique yet still sort roughly by creation time,
/// which keeps B-tree indexes compact and inserts append-friendly.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    protected Entity() { }
}

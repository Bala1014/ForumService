namespace Racinglazing.Forum.Domain.Entities;

/// <summary>Join entity linking threads and tags (many-to-many).</summary>
public class ThreadTag
{
    public Guid ThreadId { get; private set; }
    public Thread? Thread { get; private set; }

    public Guid TagId { get; private set; }
    public Tag? Tag { get; private set; }

    private ThreadTag() { }

    public ThreadTag(Guid threadId, Guid tagId)
    {
        ThreadId = threadId;
        TagId = tagId;
    }
}

namespace Racinglazing.Forum.Domain.Common;

/// <summary>
/// Implemented by entities that carry a denormalised vote tally (Thread,
/// Comment). Lets voting logic be written once against the abstraction.
/// </summary>
public interface IVotable
{
    int UpvoteCount { get; }
    int DownvoteCount { get; }
    int Score { get; }
    void ApplyVoteTotals(int upvotes, int downvotes);
}

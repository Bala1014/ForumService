namespace Racinglazing.Forum.Domain.Enums;

/// <summary>
/// The kind of entity a vote or report points at. Kept as an enum (rather than
/// separate vote tables per type) so new votable entities can be added later
/// without schema churn.
/// </summary>
public enum VoteTargetType
{
    Thread = 1,
    Comment = 2
}

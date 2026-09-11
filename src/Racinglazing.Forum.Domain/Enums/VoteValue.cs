namespace Racinglazing.Forum.Domain.Enums;

/// <summary>
/// The direction of a vote. Stored as a signed integer so that aggregate score
/// maths (score = sum(value)) stays trivial at the database level.
/// </summary>
public enum VoteValue
{
    Down = -1,
    Up = 1
}

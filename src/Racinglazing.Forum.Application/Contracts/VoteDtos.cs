namespace Racinglazing.Forum.Application.Contracts;

public sealed record VoteResultDto(
    string TargetId,
    string? Vote,
    int Score,
    int UpvoteCount,
    int DownvoteCount);

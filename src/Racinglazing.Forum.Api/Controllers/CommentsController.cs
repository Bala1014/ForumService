using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Comments;
using Racinglazing.Forum.Application.Features.Votes;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/comments")]
public class CommentsController(ICommentService comments, IVoteService votes) : ApiControllerBase
{
    /// <summary>Returns direct child replies of a comment (sort: top|new|old).</summary>
    [HttpGet("{commentId:guid}/replies")]
    public async Task<IActionResult> ListReplies(
        Guid commentId, [FromQuery] string? sort, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await comments.ListRepliesAsync(
            commentId, ApiEnums.ParseCommentSort(sort), new PageRequest(pageSize, cursor), ct));

    [HttpPost("{commentId:guid}/replies")]
    public async Task<IActionResult> CreateReply(Guid commentId, [FromBody] CreateCommentRequest body, CancellationToken ct)
    {
        var result = await comments.CreateReplyAsync(commentId, body, ct);
        return DataCreated($"/api/v1/comments/{result.Id}", result);
    }

    [HttpPut("{commentId:guid}/vote")]
    public async Task<IActionResult> Vote(Guid commentId, [FromBody] VoteRequest body, CancellationToken ct)
    {
        VoteValueExtensions.TryParse(body.Vote, out var value);
        return Data(await votes.CastAsync(VoteTargetType.Comment, commentId, value, ct));
    }

    [HttpDelete("{commentId:guid}/vote")]
    public async Task<IActionResult> RemoveVote(Guid commentId, CancellationToken ct)
        => Data(await votes.RemoveAsync(VoteTargetType.Comment, commentId, ct));
}

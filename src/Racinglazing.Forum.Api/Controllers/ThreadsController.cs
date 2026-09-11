using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Comments;
using Racinglazing.Forum.Application.Features.Threads;
using Racinglazing.Forum.Application.Features.Votes;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/threads")]
public class ThreadsController(
    IThreadService threads,
    ICommentService comments,
    IVoteService votes) : ApiControllerBase
{
    /// <summary>Returns thread details and its root comment.</summary>
    [HttpGet("{threadId:guid}")]
    public async Task<IActionResult> Get(Guid threadId, CancellationToken ct)
        => Data(await threads.GetAsync(threadId, ct));

    /// <summary>Soft-deletes a thread (author or moderator).</summary>
    [HttpDelete("{threadId:guid}")]
    public async Task<IActionResult> Delete(Guid threadId, CancellationToken ct)
        => Data(await threads.DeleteAsync(threadId, ct));

    // --- Moderation: pin / lock ---

    [HttpPut("{threadId:guid}/pin")]
    public async Task<IActionResult> Pin(Guid threadId, CancellationToken ct)
        => Data(await threads.SetPinnedAsync(threadId, true, ct));

    [HttpDelete("{threadId:guid}/pin")]
    public async Task<IActionResult> Unpin(Guid threadId, CancellationToken ct)
        => Data(await threads.SetPinnedAsync(threadId, false, ct));

    [HttpPut("{threadId:guid}/lock")]
    public async Task<IActionResult> Lock(Guid threadId, [FromBody] LockThreadRequest? body, CancellationToken ct)
        => Data(await threads.SetLockedAsync(threadId, true, body?.Reason, ct));

    [HttpDelete("{threadId:guid}/lock")]
    public async Task<IActionResult> Unlock(Guid threadId, CancellationToken ct)
        => Data(await threads.SetLockedAsync(threadId, false, null, ct));

    // --- Comments on a thread ---

    /// <summary>Returns top-level comments (sort: top|new|old).</summary>
    [HttpGet("{threadId:guid}/comments")]
    public async Task<IActionResult> ListComments(
        Guid threadId, [FromQuery] string? sort, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await comments.ListTopLevelAsync(
            threadId, ApiEnums.ParseCommentSort(sort), new PageRequest(pageSize, cursor), ct));

    [HttpPost("{threadId:guid}/comments")]
    public async Task<IActionResult> CreateComment(Guid threadId, [FromBody] CreateCommentRequest body, CancellationToken ct)
    {
        var result = await comments.CreateTopLevelAsync(threadId, body, ct);
        return DataCreated($"/api/v1/comments/{result.Id}", result);
    }

    // --- Voting on a thread ---

    [HttpPut("{threadId:guid}/vote")]
    public async Task<IActionResult> Vote(Guid threadId, [FromBody] VoteRequest body, CancellationToken ct)
    {
        VoteValueExtensions.TryParse(body.Vote, out var value);
        return Data(await votes.CastAsync(VoteTargetType.Thread, threadId, value, ct));
    }

    [HttpDelete("{threadId:guid}/vote")]
    public async Task<IActionResult> RemoveVote(Guid threadId, CancellationToken ct)
        => Data(await votes.RemoveAsync(VoteTargetType.Thread, threadId, ct));
}

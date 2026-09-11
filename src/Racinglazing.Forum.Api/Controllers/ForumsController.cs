using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Forums;
using Racinglazing.Forum.Application.Features.Threads;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/forums")]
public class ForumsController(IForumService forums, IThreadService threads) : ApiControllerBase
{
    /// <summary>Returns forums, optionally filtered by category.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? categoryId, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await forums.ListAsync(categoryId, new PageRequest(pageSize, cursor), ct));

    /// <summary>Returns forum details.</summary>
    [HttpGet("{forumId:guid}")]
    public async Task<IActionResult> Get(Guid forumId, CancellationToken ct)
        => Data(await forums.GetByIdAsync(forumId, ct));

    /// <summary>Returns threads within a forum (sort: hot|new|top|...).</summary>
    [HttpGet("{forumId:guid}/threads")]
    public async Task<IActionResult> ListThreads(
        Guid forumId, [FromQuery] string? sort, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await threads.ListByForumAsync(
            forumId, ApiEnums.ParseThreadSort(sort), new PageRequest(pageSize, cursor), ct));

    /// <summary>Creates a new thread. AuthorId comes from the authenticated user.</summary>
    [HttpPost("{forumId:guid}/threads")]
    public async Task<IActionResult> CreateThread(Guid forumId, [FromBody] CreateThreadRequest body, CancellationToken ct)
    {
        var result = await threads.CreateAsync(forumId, body, ct);
        return DataCreated($"/api/v1/threads/{result.Id}", result);
    }
}

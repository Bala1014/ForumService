using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Features.Tags;
using Racinglazing.Forum.Application.Features.Threads;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/tags")]
public class TagsController(ITagService tags, IThreadService threads) : ApiControllerBase
{
    /// <summary>Returns available tags, optionally filtered by prefix search.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await tags.SearchAsync(search, new PageRequest(pageSize, cursor), ct));

    /// <summary>Returns threads tagged with the given slug.</summary>
    [HttpGet("{tagSlug}/threads")]
    public async Task<IActionResult> Threads(
        string tagSlug, [FromQuery] string? sort, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await threads.ListByTagAsync(
            tagSlug, ApiEnums.ParseThreadSort(sort), new PageRequest(pageSize, cursor), ct));
}

using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Features.Threads;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/races")]
public class RacesController(IThreadService threads) : ApiControllerBase
{
    /// <summary>Returns threads associated with a race (RaceService owns races).</summary>
    [HttpGet("{raceId}/threads")]
    public async Task<IActionResult> Threads(
        string raceId, [FromQuery] string? sort, [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await threads.ListByRaceAsync(
            raceId, ApiEnums.ParseThreadSort(sort), new PageRequest(pageSize, cursor), ct));
}

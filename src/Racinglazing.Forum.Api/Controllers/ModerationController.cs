using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Moderation;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/moderation/reports")]
public class ModerationController(IModerationService moderation) : ApiControllerBase
{
    /// <summary>Lists reports for moderators (filter by status/targetType).</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? targetType,
        [FromQuery] int? pageSize, [FromQuery] string? cursor, CancellationToken ct)
        => Page(await moderation.ListReportsAsync(status, targetType, new PageRequest(pageSize, cursor), ct));

    /// <summary>Resolves a report (moderators only).</summary>
    [HttpPatch("{reportId:guid}")]
    public async Task<IActionResult> Resolve(Guid reportId, [FromBody] ResolveReportRequest body, CancellationToken ct)
        => Data(await moderation.ResolveAsync(reportId, body, ct));
}

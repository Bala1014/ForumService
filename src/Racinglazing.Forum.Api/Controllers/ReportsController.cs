using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Application.Features.Reports;

namespace Racinglazing.Forum.Api.Controllers;

[Route("api/v1/reports")]
public class ReportsController(IReportService reports) : ApiControllerBase
{
    /// <summary>Reports a thread or comment for moderation.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest body, CancellationToken ct)
    {
        var result = await reports.CreateAsync(body, ct);
        return DataCreated($"/api/v1/reports/{result.Id}", result);
    }
}

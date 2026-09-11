using Microsoft.AspNetCore.Mvc;
using Racinglazing.Forum.Api.Common;
using Racinglazing.Forum.Application.Common;

namespace Racinglazing.Forum.Api.Controllers;

/// <summary>
/// Base for all API controllers. Centralises the contract's response envelopes
/// so actions stay focused on delegating to the application services.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Wraps a single resource in { data }.</summary>
    protected IActionResult Data<T>(T data) => Ok(new DataEnvelope<T>(data));

    /// <summary>Wraps a created resource in { data } with a 201 + Location.</summary>
    protected IActionResult DataCreated<T>(string location, T data)
        => Created(location, new DataEnvelope<T>(data));

    /// <summary>Wraps a page in { data, pagination }.</summary>
    protected IActionResult Page<T>(PagedResult<T> page)
        => Ok(new PagedEnvelope<T>(page.Items, new PaginationInfo(page.NextCursor, page.HasMore)));
}

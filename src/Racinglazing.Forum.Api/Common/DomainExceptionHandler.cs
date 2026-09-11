using Microsoft.AspNetCore.Diagnostics;
using Racinglazing.Forum.Domain.Common;

namespace Racinglazing.Forum.Api.Common;

/// <summary>
/// Translates domain exceptions into the contract's { error: { code, message } }
/// envelope with an appropriate status code. Unexpected exceptions become a
/// generic 500 (details logged, not leaked).
/// </summary>
public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, body) = exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, new ErrorBody(e.Code, e.Message)),
            ValidationFailedException e => (StatusCodes.Status400BadRequest, new ErrorBody(e.Code, e.Message)),
            ConflictException e => (StatusCodes.Status409Conflict, new ErrorBody(e.Code, e.Message)),
            UnauthorizedException e => (StatusCodes.Status401Unauthorized, new ErrorBody(e.Code, e.Message)),
            ForbiddenException e => (StatusCodes.Status403Forbidden, new ErrorBody(e.Code, e.Message)),
            _ => (StatusCodes.Status500InternalServerError,
                  new ErrorBody("INTERNAL_ERROR", "An unexpected error occurred."))
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ErrorEnvelope(body), ct);
        return true;
    }
}

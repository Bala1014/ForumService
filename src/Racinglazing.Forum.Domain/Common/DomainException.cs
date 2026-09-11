namespace Racinglazing.Forum.Domain.Common;

/// <summary>
/// Base for expected, business-rule failures. Carries a stable error code that
/// the API maps directly into the contract's { error: { code, message } } shape.
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>A requested resource does not exist (maps to HTTP 404).</summary>
public sealed class NotFoundException(string code, string message)
    : DomainException(code, message);

/// <summary>A business rule was violated (maps to HTTP 409/400).</summary>
public sealed class ConflictException(string code, string message)
    : DomainException(code, message);

/// <summary>The request is not authenticated (maps to HTTP 401).</summary>
public sealed class UnauthorizedException(string code, string message)
    : DomainException(code, message);

/// <summary>The caller is not permitted to perform the operation (maps to 403).</summary>
public sealed class ForbiddenException(string code, string message)
    : DomainException(code, message);

/// <summary>Input failed validation (maps to HTTP 400).</summary>
public sealed class ValidationFailedException(string code, string message)
    : DomainException(code, message);

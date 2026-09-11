namespace Racinglazing.Forum.Api.Common;

// The contract's response envelopes.
public sealed record DataEnvelope<T>(T Data);
public sealed record PagedEnvelope<T>(IReadOnlyList<T> Data, PaginationInfo Pagination);
public sealed record PaginationInfo(string? NextCursor, bool HasMore);
public sealed record ErrorEnvelope(ErrorBody Error);
public sealed record ErrorBody(string Code, string Message);

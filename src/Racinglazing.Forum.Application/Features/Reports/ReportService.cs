using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Features.Reports;

public interface IReportService
{
    Task<ReportDto> CreateAsync(CreateReportRequest request, CancellationToken ct = default);
}

public sealed class ReportService(
    IReportRepository reports,
    IThreadRepository threads,
    ICommentRepository comments,
    IUnitOfWork uow,
    ICurrentUser currentUser) : IReportService
{
    public async Task<ReportDto> CreateAsync(CreateReportRequest request, CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();

        var targetType = ApiEnums.ParseTargetType(request.TargetType);
        var reason = ApiEnums.ParseReason(request.Reason);

        if (!Guid.TryParse(request.TargetId, out var targetId))
            throw new ValidationFailedException("INVALID_TARGET_ID", "targetId is not a valid identifier.");

        await EnsureTargetExistsAsync(targetType, targetId, ct);

        var report = new Report(targetType, targetId, userId, reason, request.Description?.Trim());
        reports.Add(report);
        await uow.SaveChangesAsync(ct);

        return new ReportDto(
            report.Id.ToString(), targetType.ToApiString(), targetId.ToString(),
            reason.ToApiString(), report.Status.ToApiString(), report.CreatedAt);
    }

    private async Task EnsureTargetExistsAsync(VoteTargetType targetType, Guid targetId, CancellationToken ct)
    {
        var exists = targetType switch
        {
            VoteTargetType.Thread => await threads.GetByIdAsync(targetId, ct: ct) is not null,
            VoteTargetType.Comment => await comments.GetByIdAsync(targetId, ct) is not null,
            _ => false
        };
        if (!exists)
            throw new NotFoundException("TARGET_NOT_FOUND", "The reported target was not found.");
    }
}

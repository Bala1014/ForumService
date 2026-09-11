using Racinglazing.Forum.Application.Abstractions.Identity;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Contracts;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Enums;

namespace Racinglazing.Forum.Application.Features.Moderation;

public interface IModerationService
{
    Task<PagedResult<ModerationReportDto>> ListReportsAsync(
        string? status, string? targetType, PageRequest page, CancellationToken ct = default);

    Task<ResolveReportResultDto> ResolveAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct = default);
}

public sealed class ModerationService(
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IUserProfileProvider userProfiles,
    IDateTimeProvider clock) : IModerationService
{
    public async Task<PagedResult<ModerationReportDto>> ListReportsAsync(
        string? status, string? targetType, PageRequest page, CancellationToken ct = default)
    {
        RequireModerator();

        var statusFilter = ApiEnums.ParseStatusOrNull(status);
        var typeFilter = string.IsNullOrWhiteSpace(targetType) ? (VoteTargetType?)null : ApiEnums.ParseTargetType(targetType);

        var result = await reports.ListAsync(statusFilter, typeFilter, page, ct);

        var reporterIds = result.Items.Select(r => r.ReportedByUserId).Distinct().ToList();
        var profiles = reporterIds.Count == 0
            ? new Dictionary<string, UserRefDto>()
            : await userProfiles.GetProfilesAsync(reporterIds, ct);

        var items = result.Items.Select(r =>
        {
            var reporter = profiles.TryGetValue(r.ReportedByUserId, out var u)
                ? u : ContractMapper.UnknownUser(r.ReportedByUserId);
            return new ModerationReportDto(
                r.Id.ToString(), r.TargetType.ToApiString(), r.TargetId.ToString(), reporter,
                r.Reason.ToApiString(), r.Description, r.Status.ToApiString(), r.CreatedAt);
        }).ToList();

        return new PagedResult<ModerationReportDto>(items, result.NextCursor, result.HasMore);
    }

    public async Task<ResolveReportResultDto> ResolveAsync(Guid reportId, ResolveReportRequest request, CancellationToken ct = default)
    {
        var moderatorId = RequireModerator();

        var report = await reports.GetByIdAsync(reportId, ct)
            ?? throw new NotFoundException("REPORT_NOT_FOUND", "The requested report was not found.");

        var status = ApiEnums.ParseStatus(request.Status);
        var action = ApiEnums.ParseAction(request.Action);

        report.Resolve(status, action, request.Notes?.Trim(), moderatorId, clock.UtcNow);
        await uow.SaveChangesAsync(ct);

        return new ResolveReportResultDto(
            report.Id.ToString(), report.Status.ToApiString(), report.Action.ToApiString(),
            report.Notes, report.ResolvedAt, report.ResolvedByUserId);
    }

    private string RequireModerator()
    {
        var userId = currentUser.RequireUserId();
        if (!currentUser.IsModerator)
            throw new ForbiddenException("FORBIDDEN", "This operation requires moderator privileges.");
        return userId;
    }
}

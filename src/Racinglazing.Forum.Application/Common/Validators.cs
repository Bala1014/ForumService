using FluentValidation;
using Racinglazing.Forum.Application.Contracts;

namespace Racinglazing.Forum.Application.Common;

public sealed class CreateThreadRequestValidator : AbstractValidator<CreateThreadRequest>
{
    public const int MaxTitle = 300;
    public const int MaxBody = 40_000;
    public const int MaxTags = 10;

    public CreateThreadRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(MaxTitle);
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Content.Text)
            .NotEmpty().MaximumLength(MaxBody)
            .When(x => x.Content is not null);
        RuleFor(x => x.Tags!)
            .Must(t => t.Count <= MaxTags).WithMessage($"At most {MaxTags} tags are allowed.")
            .When(x => x.Tags is not null);
        RuleForEach(x => x.Tags!).NotEmpty().MaximumLength(50).When(x => x.Tags is not null);
    }
}

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public const int MaxBody = 40_000;

    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Content.Text)
            .NotEmpty().MaximumLength(MaxBody)
            .When(x => x.Content is not null);
    }
}

public sealed class VoteRequestValidator : AbstractValidator<VoteRequest>
{
    public VoteRequestValidator()
    {
        RuleFor(x => x.Vote)
            .NotEmpty()
            .Must(v => VoteValueExtensions.TryParse(v, out _))
            .WithMessage("vote must be 'up' or 'down'.");
    }
}

public sealed class LockThreadRequestValidator : AbstractValidator<LockThreadRequest>
{
    public LockThreadRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.TargetType).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class ResolveReportRequestValidator : AbstractValidator<ResolveReportRequest>
{
    public ResolveReportRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

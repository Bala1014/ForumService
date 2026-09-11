using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Racinglazing.Forum.Application.Common;
using Racinglazing.Forum.Application.Features.Categories;
using Racinglazing.Forum.Application.Features.Comments;
using Racinglazing.Forum.Application.Features.Forums;
using Racinglazing.Forum.Application.Features.Moderation;
using Racinglazing.Forum.Application.Features.Reports;
using Racinglazing.Forum.Application.Features.Tags;
using Racinglazing.Forum.Application.Features.Threads;
using Racinglazing.Forum.Application.Features.Votes;

namespace Racinglazing.Forum.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ResponseEnricher>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IForumService, ForumService>();
        services.AddScoped<IThreadService, ThreadService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IModerationService, ModerationService>();

        services.AddValidatorsFromAssemblyContaining<CreateThreadRequestValidator>();

        return services;
    }
}

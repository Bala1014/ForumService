using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Application.Abstractions.Services;
using Racinglazing.Forum.Infrastructure.Persistence;
using Racinglazing.Forum.Infrastructure.Persistence.Repositories;
using Racinglazing.Forum.Infrastructure.Services;

namespace Racinglazing.Forum.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ForumDatabase")
            ?? throw new InvalidOperationException("Connection string 'ForumDatabase' was not configured.");

        services.AddDbContext<ForumDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ForumDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        // Expose the DbContext as the unit of work for the application layer.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ForumDbContext>());

        // Repositories.
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IForumRepository, ForumRepository>();
        services.AddScoped<IThreadRepository, ThreadRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        // Cross-cutting services.
        services.AddSingleton<ISlugGenerator, SlugGenerator>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // External-service integrations (stubbed until the services exist).
        services.AddScoped<IUserProfileProvider, StubUserProfileProvider>();
        services.AddScoped<IRaceServiceClient, StubRaceServiceClient>();

        return services;
    }
}

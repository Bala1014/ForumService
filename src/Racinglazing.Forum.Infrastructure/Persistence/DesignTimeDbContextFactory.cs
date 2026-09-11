using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Racinglazing.Forum.Infrastructure.Persistence;

/// <summary>
/// Used by the EF Core CLI (dotnet ef migrations/database) at design time. The
/// connection string here is only for generating migrations; the running app
/// supplies its own from configuration.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ForumDbContext>
{
    public ForumDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FORUM_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=forumservice;Username=forum;Password=forum";

        var options = new DbContextOptionsBuilder<ForumDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(ForumDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ForumDbContext(options);
    }
}

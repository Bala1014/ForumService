using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Application.Abstractions.Persistence;
using Racinglazing.Forum.Domain.Entities;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Infrastructure.Persistence;

public sealed class ForumDbContext(DbContextOptions<ForumDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Domain.Entities.Forum> Forums => Set<Domain.Entities.Forum>();
    public DbSet<DomainThread> Threads => Set<DomainThread>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ThreadTag> ThreadTags => Set<ThreadTag>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // IUnitOfWork.SaveChangesAsync is satisfied by DbContext.SaveChangesAsync.
}

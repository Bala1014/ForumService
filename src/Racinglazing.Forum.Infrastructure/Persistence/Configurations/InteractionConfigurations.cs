using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.Forum.Domain.Entities;

namespace Racinglazing.Forum.Infrastructure.Persistence.Configurations;

public sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> b)
    {
        b.ToTable("votes");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        b.Property(x => x.Value).HasConversion<int>();
        b.Property(x => x.TargetType).HasConversion<int>();

        // At most one vote per (user, target); also the lookup path for casting.
        b.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId }).IsUnique();
        // Fetch a user's votes across a set of targets (userVote enrichment).
        b.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.ToTable("reports");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReportedByUserId).HasMaxLength(128).IsRequired();
        b.Property(x => x.ResolvedByUserId).HasMaxLength(128);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.TargetType).HasConversion<int>();
        b.Property(x => x.Reason).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Action).HasConversion<int>();

        // Moderation queue: filter by status/type, newest first.
        b.HasIndex(x => new { x.Status, x.TargetType, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_reports_queue");
    }
}

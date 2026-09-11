using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.Forum.Domain.Entities;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;

namespace Racinglazing.Forum.Infrastructure.Persistence.Configurations;

public sealed class ThreadConfiguration : IEntityTypeConfiguration<DomainThread>
{
    public void Configure(EntityTypeBuilder<DomainThread> b)
    {
        b.ToTable("threads");
        b.HasKey(x => x.Id);

        b.Property(x => x.AuthorId).HasMaxLength(128).IsRequired();
        b.Property(x => x.RaceId).HasMaxLength(128);
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(340).IsRequired();
        b.Property(x => x.LockReason).HasMaxLength(500);

        b.HasOne(x => x.Forum)
            .WithMany()
            .HasForeignKey(x => x.ForumId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ForumId, x.Slug }).IsUnique();

        // Feed indexes: pinned-first, then the sort key, then id as tiebreaker.
        // These make each sort a plain indexed ORDER BY + keyset scan.
        b.HasIndex(x => new { x.ForumId, x.IsDeleted, x.IsPinned, x.HotScore, x.Id })
            .HasDatabaseName("ix_threads_forum_hot");
        b.HasIndex(x => new { x.ForumId, x.IsDeleted, x.IsPinned, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_threads_forum_new");
        b.HasIndex(x => new { x.ForumId, x.IsDeleted, x.IsPinned, x.Score, x.Id })
            .HasDatabaseName("ix_threads_forum_top");
        b.HasIndex(x => new { x.ForumId, x.IsDeleted, x.IsPinned, x.LastActivityAt, x.Id })
            .HasDatabaseName("ix_threads_forum_updated");
        b.HasIndex(x => new { x.ForumId, x.IsDeleted, x.IsPinned, x.CommentCount, x.Id })
            .HasDatabaseName("ix_threads_forum_commented");

        // Race-scoped feed.
        b.HasIndex(x => new { x.RaceId, x.IsDeleted, x.IsPinned, x.HotScore, x.Id })
            .HasDatabaseName("ix_threads_race_hot");
    }
}

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> b)
    {
        b.ToTable("comments");
        b.HasKey(x => x.Id);

        b.Property(x => x.AuthorId).HasMaxLength(128).IsRequired();

        // Body as an owned value object -> content_text column. Extensible to
        // add media metadata later without reshaping the row for callers.
        b.OwnsOne(x => x.Content, c =>
        {
            c.Property(p => p.Text).HasColumnName("content_text").HasMaxLength(40_000).IsRequired();
        });

        b.HasOne<DomainThread>()
            .WithMany()
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Top-level comments listing (parent IS NULL AND NOT is_root), sorted.
        b.HasIndex(x => new { x.ThreadId, x.ParentCommentId, x.IsRoot, x.Score, x.Id })
            .HasDatabaseName("ix_comments_thread_top");
        b.HasIndex(x => new { x.ThreadId, x.ParentCommentId, x.IsRoot, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_comments_thread_new");
        // Replies listing (parent = X), sorted.
        b.HasIndex(x => new { x.ParentCommentId, x.Score, x.Id })
            .HasDatabaseName("ix_comments_replies_top");
        b.HasIndex(x => new { x.ParentCommentId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_comments_replies_new");
    }
}

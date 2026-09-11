using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.Forum.Domain.Entities;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.DisplayOrder });

        b.HasMany(x => x.Forums)
            .WithOne(f => f.Category)
            .HasForeignKey(f => f.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ForumConfiguration : IEntityTypeConfiguration<DomainForum>
{
    public void Configure(EntityTypeBuilder<DomainForum> b)
    {
        b.ToTable("forums");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.CategoryId, x.DisplayOrder });
        b.HasIndex(x => new { x.CategoryId, x.Slug }).IsUnique();
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        // Supports prefix search (ILIKE 'foo%') and ordering by popularity.
        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.ThreadCount);
    }
}

public sealed class ThreadTagConfiguration : IEntityTypeConfiguration<ThreadTag>
{
    public void Configure(EntityTypeBuilder<ThreadTag> b)
    {
        b.ToTable("thread_tags");
        b.HasKey(x => new { x.ThreadId, x.TagId });
        b.HasIndex(x => x.TagId);

        b.HasOne(x => x.Thread)
            .WithMany(t => t.ThreadTags)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

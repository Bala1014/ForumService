using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Domain.Entities;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Infrastructure.Persistence;

/// <summary>
/// Seeds the MVP taxonomy (categories + forums). Idempotent: it only inserts
/// categories that don't already exist by slug.
/// </summary>
public static class ForumDbSeeder
{
    public static async Task SeedAsync(ForumDbContext db, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        // MVP-active categories. MotoGP / IndyCar / NASCAR are intentionally
        // left out for now (marked "later" in the spec) but can be added
        // without any schema change.
        var f1 = new Category("Formula 1", "formula-1", "Formula 1 discussions", 1);
        var wec = new Category("WEC", "wec", "World Endurance Championship discussions", 2);
        var general = new Category("General Motorsport", "general-motorsport", "Everything motorsport", 3);

        db.Categories.AddRange(f1, wec, general);

        AddForums(db, f1,
            ("General Discussion", "general-discussion", "General Formula 1 discussion"),
            ("Race Discussion", "race-discussion", "Race weekend discussions"),
            ("Technical Discussion", "technical-discussion", "Cars, regulations and engineering"),
            ("Driver Discussion", "driver-discussion", "Drivers and line-ups"));

        AddForums(db, wec,
            ("General Discussion", "general-discussion", "General WEC discussion"),
            ("Race Discussion", "race-discussion", "Race weekend discussions"));

        AddForums(db, general,
            ("Paddock Club", "paddock-club", "Off-topic and general chat"));

        await db.SaveChangesAsync(ct);
    }

    private static void AddForums(ForumDbContext db, Category category,
        params (string Name, string Slug, string Description)[] forums)
    {
        var order = 1;
        foreach (var (name, slug, description) in forums)
        {
            db.Forums.Add(new DomainForum(category.Id, name, slug, description, order++));
            category.RegisterForumAdded();
        }
    }
}

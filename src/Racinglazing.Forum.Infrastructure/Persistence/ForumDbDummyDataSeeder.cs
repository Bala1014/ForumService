using Microsoft.EntityFrameworkCore;
using Racinglazing.Forum.Domain.Common;
using Racinglazing.Forum.Domain.Entities;
using Racinglazing.Forum.Domain.Enums;
using DomainThread = Racinglazing.Forum.Domain.Entities.Thread;
using DomainForum = Racinglazing.Forum.Domain.Entities.Forum;

namespace Racinglazing.Forum.Infrastructure.Persistence;

/// <summary>
/// Seeds a realistic volume of threads, comments, tags, votes and reports on
/// top of the base taxonomy so the frontend team has real data to build
/// against: every sort order, pagination (one forum deliberately exceeds a
/// page), and moderation state (pinned/locked/deleted/reported) is
/// represented. Idempotent: skipped once any thread exists. Uses a fixed
/// random seed so the dataset is identical across redeploys.
///
/// CreatedAt/HotScore can't be backdated through the domain API (by design —
/// creation time is real for actual usage), so after saving, a raw-SQL pass
/// rewrites those two columns to give the data a realistic time spread.
/// </summary>
public static class ForumDbDummyDataSeeder
{
    private static readonly string[] Authors =
    [
        "u_kavi_r", "u_meera_s", "u_daniel_p", "u_carlos_m", "u_yuki_t", "u_ana_f",
        "u_leo_h", "u_priya_k", "u_tom_b", "u_haruto_n", "u_noor_z", "u_felix_w",
        "u_grace_o", "u_sam_q"
    ];

    private static readonly string[] Moderators = ["mod_asha", "mod_diego"];

    private static readonly (string Name, string Slug)[] TagDefs =
    [
        ("Qualifying", "qualifying"),
        ("Race Strategy", "race-strategy"),
        ("Power Unit", "power-unit"),
        ("Silly Season", "silly-season"),
        ("Stewards & Penalties", "stewards-penalties"),
        ("Rules & Regulations", "rules-regulations"),
        ("Rumour Mill", "rumour-mill"),
        ("Fan Content", "fan-content"),
        ("Off Topic", "off-topic"),
        ("Technical Analysis", "technical-analysis"),
        ("Concept Cars", "concept-cars"), // deliberately left unattached to any thread
    ];

    private static readonly string[] BulkGpNames =
    [
        "Bahrain", "Saudi Arabian", "Australian", "Japanese", "Chinese", "Miami",
        "Emilia Romagna", "Monaco", "Canadian", "Spanish", "Austrian", "British",
        "Belgian", "Hungarian", "Dutch", "Italian", "Azerbaijan", "Singapore",
        "United States", "Mexico City", "Las Vegas", "Abu Dhabi", "Portuguese"
    ];

    private static readonly string[] Drivers =
        ["Max", "Lando", "Charles", "Carlos", "Lewis", "George", "Oscar", "Fernando", "Yuki", "Alex"];

    private static readonly string[] ReactionTemplates =
    [
        "What a race! {0} nailed the strategy today.",
        "That safety car really changed everything for {0}.",
        "Can't believe {0} pulled that overtake off.",
        "Team radio from {0}'s car was gold this weekend.",
        "Strategy call on {0} was way too conservative.",
        "{0} deserved better today, tough luck.",
        "Best race of the season so far, {0} was on fire.",
        "Tyre degradation looked brutal for {0} in the second stint.",
        "Honestly a fairly forgettable race apart from {0}.",
        "Qualifying result really set this one up for {0}."
    ];

    public static async Task SeedAsync(ForumDbContext db, CancellationToken ct = default)
    {
        if (await db.Threads.AnyAsync(ct)) return;

        var forums = await db.Forums.Include(f => f.Category).ToListAsync(ct);
        DomainForum Forum(string categorySlug, string forumSlug) =>
            forums.First(f => f.Category!.Slug == categorySlug && f.Slug == forumSlug);

        var tags = TagDefs.ToDictionary(t => t.Slug, t => new Tag(t.Name, t.Slug));
        db.Tags.AddRange(tags.Values);

        var voters = Enumerable.Range(1, 100).Select(i => $"voter-{i:000}").ToArray();
        var votePool = Authors.Concat(Moderators).Concat(voters).ToArray();

        var rng = new Random(913200613);
        var now = DateTimeOffset.UtcNow;

        var threadCreatedAt = new Dictionary<Guid, DateTimeOffset>();
        var commentCreatedAt = new Dictionary<Guid, DateTimeOffset>();
        var reportCreatedAt = new Dictionary<Guid, DateTimeOffset>();
        var allThreads = new List<DomainThread>();
        var allComments = new List<Comment>();
        var allReports = new List<Report>();

        // --- local helpers -------------------------------------------------

        string Slugify(string title)
        {
            var s = new string(title.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
            while (s.Contains("--")) s = s.Replace("--", "-");
            return s.Trim('-');
        }

        void CastVotes(Guid targetId, VoteTargetType type, IVotable target, int up, int down)
        {
            if (up + down == 0) return;
            var pool = votePool.OrderBy(_ => rng.Next()).Take(up + down).ToArray();
            for (var i = 0; i < up; i++) db.Votes.Add(new Vote(pool[i], type, targetId, VoteValue.Up));
            for (var i = 0; i < down; i++) db.Votes.Add(new Vote(pool[up + i], type, targetId, VoteValue.Down));
            target.ApplyVoteTotals(up, down);
        }

        (DomainThread Thread, Comment Root) NewThread(
            DomainForum forum, string authorId, string title, string body,
            DateTimeOffset createdAt, string? raceId = null, IEnumerable<string>? tagSlugs = null)
        {
            var category = forum.Category!;
            var thread = new DomainThread(forum.Id, authorId, title, Slugify(title), raceId);
            var root = Comment.CreateRoot(thread.Id, authorId, new CommentContent(body));
            thread.AttachRootComment(root.Id);

            foreach (var slug in tagSlugs ?? [])
            {
                var tag = tags[slug];
                tag.RegisterThreadAdded();
                thread.AddTag(tag.Id);
            }

            forum.RegisterThreadAdded(createdAt);
            forum.RegisterPostAdded(createdAt);
            category.RegisterThreadAdded();

            db.Threads.Add(thread);
            db.Comments.Add(root);

            threadCreatedAt[thread.Id] = createdAt;
            commentCreatedAt[root.Id] = createdAt;
            allThreads.Add(thread);
            allComments.Add(root);

            return (thread, root);
        }

        Comment AddTopLevel(DomainForum forum, DomainThread thread, string authorId, string body, DateTimeOffset createdAt)
        {
            var comment = Comment.CreateTopLevel(thread.Id, authorId, new CommentContent(body));
            db.Comments.Add(comment);
            thread.RegisterCommentAdded(createdAt);
            forum.RegisterPostAdded(createdAt);
            commentCreatedAt[comment.Id] = createdAt;
            allComments.Add(comment);
            return comment;
        }

        Comment AddReply(DomainForum forum, DomainThread thread, Comment parent, string authorId, string body, DateTimeOffset createdAt)
        {
            var reply = Comment.CreateReply(thread.Id, parent, authorId, new CommentContent(body));
            db.Comments.Add(reply);
            parent.RegisterReplyAdded();
            thread.RegisterCommentAdded(createdAt);
            forum.RegisterPostAdded(createdAt);
            commentCreatedAt[reply.Id] = createdAt;
            allComments.Add(reply);
            return reply;
        }

        void SoftDeleteThread(DomainForum forum, DomainThread thread, DateTimeOffset at)
        {
            thread.SoftDelete(at);
            forum.RegisterThreadRemoved();
            forum.Category?.RegisterThreadRemoved();
        }

        Report AddReport(
            VoteTargetType type, Guid targetId, string reporterId, ReportReason reason, string? description,
            DateTimeOffset createdAt, ReportStatus status = ReportStatus.Pending,
            ModerationActionType action = ModerationActionType.None, string? notes = null,
            string? resolvedBy = null, DateTimeOffset? resolvedAt = null)
        {
            var report = new Report(type, targetId, reporterId, reason, description);
            if (status != ReportStatus.Pending)
                report.Resolve(status, action, notes, resolvedBy!, resolvedAt ?? now);
            db.Reports.Add(report);
            reportCreatedAt[report.Id] = createdAt;
            allReports.Add(report);
            return report;
        }

        string RandomDriver() => Drivers[rng.Next(Drivers.Length)];
        string RandomReaction() => string.Format(ReactionTemplates[rng.Next(ReactionTemplates.Length)], RandomDriver());

        // === F1 / General Discussion ========================================
        var f1General = Forum("formula-1", "general-discussion");
        {
            var (t, _) = NewThread(f1General, Moderators[0],
                "Welcome to F1 General Discussion — read this first",
                "Quick guidelines before you post: keep it civil, no spoilers outside race threads " +
                "without a tag, and use the report button for anything that breaks the rules. Glad to have you here!",
                now.AddDays(-30), tagSlugs: ["off-topic"]);
            t.Pin();
            AddTopLevel(f1General, t, Authors[6], "Thanks for the clear rules, appreciate it.", now.AddDays(-29));
            AddTopLevel(f1General, t, Authors[9], "Is there a Discord for this community too?", now.AddDays(-27));
            CastVotes(t.Id, VoteTargetType.Thread, t, 18, 1);
        }
        {
            var (t, root) = NewThread(f1General, Authors[6],
                "Best F1 season you've ever watched?",
                "For me it's got to be 2021 — the whole title fight down to the last corner. What's yours and why?",
                now.AddDays(-9), tagSlugs: ["fan-content", "off-topic"]);
            var texts = new[]
            {
                "2008 for the sheer chaos of the last race.", "2012 had six different winners in the first six races!",
                "Gotta say 2021, controversial ending and all.", "2019 doesn't get enough love honestly.",
                "2007 McLaren vs Ferrari vs the FIA was wild.", "2010 — four drivers in with a shot at the last race.",
                "Underrated pick: 2017 Mercedes vs Ferrari."
            };
            Comment? threadForReply = null;
            foreach (var text in texts)
            {
                var c = AddTopLevel(f1General, t, Authors[rng.Next(Authors.Length)], text, now.AddDays(-9).AddHours(rng.Next(1, 200)));
                threadForReply ??= c;
            }
            if (threadForReply is not null)
            {
                var reply = AddReply(f1General, t, threadForReply, Authors[3], "Same, that season had everything.", now.AddDays(-8));
                AddReply(f1General, t, reply, Authors[7], "Agreed, rewatched the whole thing last winter.", now.AddDays(-7));
            }
            CastVotes(t.Id, VoteTargetType.Thread, t, 22, 3);
            CastVotes(root.Id, VoteTargetType.Comment, root, 6, 0);
        }
        {
            var (t, _) = NewThread(f1General, Authors[7],
                "Controversial take: current era of F1 is the best era",
                "Bigger grids, closer racing since the 2022 regs, more storylines than ever. Fight me in the comments.",
                now.AddDays(-4), tagSlugs: ["off-topic"]);
            var comments = new List<Comment>();
            var texts = new[]
            {
                "Hard disagree, the cars are too easy to drive now.", "Racing is genuinely closer than it's been in a decade though.",
                "Prefer the V10/V12 sound era personally, nothing beats it.", "Overtakes are real overtakes now, not just DRS trains.",
                "The porpoising years were rough to sit through.", "Depends what you value — noise vs. wheel-to-wheel racing.",
                "Cost cap has made things more competitive, that's undeniable.", "I miss unpredictable strategy calls from a decade ago.",
                "This era has the best driver lineup in years, easily.", "Sprint races are still a mixed bag for me."
            };
            foreach (var text in texts)
                comments.Add(AddTopLevel(f1General, t, Authors[rng.Next(Authors.Length)], text, now.AddDays(-4).AddHours(rng.Next(1, 90))));
            CastVotes(t.Id, VoteTargetType.Thread, t, 34, 31);
            CastVotes(comments[2].Id, VoteTargetType.Comment, comments[2], 14, 13);
        }
        {
            var (t, _) = NewThread(f1General, Authors[12],
                "F1 merchandise & fan content thread",
                "Drop pictures of your gear, fan art, whatever you've got. Team caps count too.",
                now.AddDays(-6), tagSlugs: ["fan-content"]);
            AddTopLevel(f1General, t, Authors[4], "My garage-scale model collection is getting out of hand.", now.AddDays(-5));
            AddTopLevel(f1General, t, Authors[8], "Team merch prices are getting a bit much these days.", now.AddDays(-5));
            AddTopLevel(f1General, t, Authors[1], "Anyone know a good print shop for driver posters?", now.AddDays(-4));
            CastVotes(t.Id, VoteTargetType.Thread, t, 9, 0);
        }
        {
            var (t, _) = NewThread(f1General, Authors[13],
                "Off-season boredom — what are you all doing",
                "Testing is still weeks away. How is everyone filling the gap this year?",
                now.AddDays(-2), tagSlugs: ["off-topic"]);
            AddTopLevel(f1General, t, Authors[5], "Rewatching classic seasons on the archive stream.", now.AddDays(-1));
            AddTopLevel(f1General, t, Authors[0], "Sim racing league with some friends, keeps the itch away.", now.AddDays(-1));
            CastVotes(t.Id, VoteTargetType.Thread, t, 4, 1);
        }
        {
            var (t, _) = NewThread(f1General, Authors[8],
                "This forum needs a dark mode",
                "Not really on-topic I know, but had to ask — any plans for a theme toggle?",
                now.AddDays(-11), tagSlugs: []);
            AddTopLevel(f1General, t, Authors[2], "Seconding this, would use it daily.", now.AddDays(-10));
            CastVotes(t.Id, VoteTargetType.Thread, t, 1, 2);
            var removedAt = now.AddDays(-1);
            AddReport(VoteTargetType.Thread, t.Id, Authors[10], ReportReason.Spam,
                "Off-topic and borderline advertising for a competing product.", now.AddDays(-2),
                status: ReportStatus.ActionTaken, action: ModerationActionType.ContentRemoved,
                notes: "Confirmed off-topic/spam, removed.", resolvedBy: Moderators[0], resolvedAt: removedAt);
            SoftDeleteThread(f1General, t, removedAt);
        }

        // === F1 / Race Discussion ==========================================
        var f1Race = Forum("formula-1", "race-discussion");
        {
            // Flagship "hot" thread: recent, huge engagement, deep nested replies.
            var (t, _) = NewThread(f1Race, Authors[2],
                "São Paulo GP — Race Thread (LIVE reactions)",
                "Lights out in Interlagos! Drop your live thoughts here as the race unfolds.",
                now.AddHours(-24), raceId: "2026-r22-brazil", tagSlugs: ["race-strategy", "stewards-penalties"]);
            Comment? deepA = null;
            for (var i = 0; i < 14; i++)
            {
                var c = AddTopLevel(f1Race, t, Authors[rng.Next(Authors.Length)], RandomReaction(), now.AddHours(-24).AddMinutes(rng.Next(5, 1400)));
                deepA ??= c;
            }
            if (deepA is not null)
            {
                var r1 = AddReply(f1Race, t, deepA, Authors[3], "Interlagos always delivers, wet-dry races are the best.", now.AddHours(-23));
                var r2 = AddReply(f1Race, t, r1, Authors[9], "Facts, half the grid always gets the strategy wrong here.", now.AddHours(-22));
                AddReply(f1Race, t, r2, Authors[11], "Remember 2021? Still one of the best drives ever.", now.AddHours(-21));
            }
            CastVotes(t.Id, VoteTargetType.Thread, t, 88, 5);
        }
        {
            // Locked thread, tied to a resolved "thread locked" moderation report.
            var (t, _) = NewThread(f1Race, Moderators[1],
                "Race Thread: Qatar GP [CLOSED - see summary]",
                "Race thread for the Qatar GP. Keep it on-topic please.",
                now.AddDays(-8), raceId: "2026-r21-qatar", tagSlugs: ["race-strategy"]);
            for (var i = 0; i < 6; i++)
                AddTopLevel(f1Race, t, Authors[rng.Next(Authors.Length)], RandomReaction(), now.AddDays(-8).AddHours(rng.Next(1, 20)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 40, 2);
            var lockedAt = now.AddDays(-7);
            t.Lock("Race concluded — discussion moved to the weekly summary thread.", lockedAt);
            AddReport(VoteTargetType.Thread, t.Id, Authors[4], ReportReason.Harassment,
                "Thread turned into personal arguments after the red flag.", now.AddDays(-7).AddHours(-2),
                status: ReportStatus.ActionTaken, action: ModerationActionType.ThreadLocked,
                notes: "Thread devolved into arguments post-race; locked and redirected to the summary thread.",
                resolvedBy: Moderators[1], resolvedAt: lockedAt);
        }
        {
            // Big-volume thread: >20 top-level comments to force pagination.
            var (t, _) = NewThread(f1Race, Authors[10],
                "Silly season mega-thread: 2027 driver line-up predictions",
                "One thread to rule them all — post your predicted 2027 grid here, we'll revisit in December.",
                now.AddDays(-15), tagSlugs: ["silly-season", "rumour-mill"]);
            Comment? first = null;
            Comment? second = null;
            for (var i = 0; i < 25; i++)
            {
                var text = $"My prediction #{i + 1}: {RandomDriver()} moves seats, calling it now.";
                var c = AddTopLevel(f1Race, t, Authors[rng.Next(Authors.Length)], text, now.AddDays(-15).AddHours(rng.Next(1, 350)));
                first ??= c;
                if (i == 5) second = c;
            }
            if (first is not null) AddReply(f1Race, t, first, Authors[1], "Bold prediction, remind me to check back in December.", now.AddDays(-14));
            if (second is not null) AddReply(f1Race, t, second, Authors[5], "No shot, contract's locked in through 2028.", now.AddDays(-13));
            CastVotes(t.Id, VoteTargetType.Thread, t, 52, 9);
        }
        {
            // Bulk race-weekend threads (also feeds ListByRaceAsync with 23 distinct races).
            for (var i = 0; i < BulkGpNames.Length; i++)
            {
                var round = i + 1;
                var gp = BulkGpNames[i];
                var createdAt = now.AddDays(-(150 - round * 6)).AddHours(-rng.Next(0, 24));
                var raceId = $"2026-r{round:00}-{Slugify(gp)}";
                var author = Authors[rng.Next(Authors.Length)];
                var tagChoices = new[] { "race-strategy", "stewards-penalties", "qualifying" };
                var threadTags = tagChoices.Where(_ => rng.NextDouble() < 0.4).ToArray();

                var (t, _) = NewThread(f1Race, author,
                    $"Round {round} — {gp} GP Race Thread",
                    $"Official race thread for the {gp} Grand Prix. Post your reactions as the race unfolds.",
                    createdAt, raceId: raceId, tagSlugs: threadTags);

                var commentCount = rng.Next(1, 6);
                for (var c = 0; c < commentCount; c++)
                    AddTopLevel(f1Race, t, Authors[rng.Next(Authors.Length)], RandomReaction(), createdAt.AddHours(rng.Next(1, 48)));

                CastVotes(t.Id, VoteTargetType.Thread, t, rng.Next(3, 26), rng.Next(0, 7));
            }
        }

        // === F1 / Technical Discussion ======================================
        var f1Tech = Forum("formula-1", "technical-discussion");
        {
            var (t, _) = NewThread(f1Tech, Authors[9],
                "2026 regulation changes — active aero explained",
                "Breaking down the active aero rules for anyone who hasn't followed the technical regs closely.",
                now.AddDays(-18), tagSlugs: ["technical-analysis", "power-unit", "rules-regulations"]);
            AddTopLevel(f1Tech, t, Authors[0], "The drag reduction mode is basically DRS built into the car now.", now.AddDays(-17));
            var editable = AddTopLevel(f1Tech, t, Authors[3], "Not sure this actually reduces overtakes complaints.", now.AddDays(-16));
            AddTopLevel(f1Tech, t, Authors[11], "Power unit split is finally closer between manufacturers.", now.AddDays(-15));
            AddTopLevel(f1Tech, t, Authors[6], "Curious how this affects tyre degradation models.", now.AddDays(-14));
            AddTopLevel(f1Tech, t, Authors[12], "Good summary, cleared up a few things for me.", now.AddDays(-13));
            editable.Edit(new CommentContent("Edit: actually this does seem to have reduced follow-the-leader racing, retracting my point."), now.AddDays(-15));
            CastVotes(t.Id, VoteTargetType.Thread, t, 27, 2);
        }
        {
            var (t, _) = NewThread(f1Tech, Authors[11],
                "Which team has the best power unit right now?",
                "Ignoring chassis for a second — purely on power unit performance, who's ahead?",
                now.AddDays(-5), tagSlugs: ["power-unit", "technical-analysis"]);
            AddTopLevel(f1Tech, t, Authors[2], "Hard to separate them without dyno numbers, but straight-line data suggests one is ahead.", now.AddDays(-4));
            AddTopLevel(f1Tech, t, Authors[5], "Reliability matters just as much as raw output here.", now.AddDays(-4));
            AddTopLevel(f1Tech, t, Authors[7], "Deployment strategy differences make this hard to compare fairly.", now.AddDays(-3));
            AddTopLevel(f1Tech, t, Authors[10], "Fuel efficiency numbers this year have been wild.", now.AddDays(-2));
            CastVotes(t.Id, VoteTargetType.Thread, t, 11, 4);
        }
        {
            var (t, _) = NewThread(f1Tech, Authors[0],
                "Concept: what would a fully solid-state battery hybrid system look like?",
                "Purely hypothetical, but with solid-state battery progress in road cars, could this ever reach F1 power units?",
                now.AddDays(-3), tagSlugs: ["technical-analysis"]);
            AddTopLevel(f1Tech, t, Authors[4], "Weight would still be the limiting factor for a while yet.", now.AddDays(-2));
            AddTopLevel(f1Tech, t, Authors[9], "Interesting thought experiment, probably a decade+ away realistically.", now.AddDays(-2));
            CastVotes(t.Id, VoteTargetType.Thread, t, 6, 0);
        }
        {
            var (t, _) = NewThread(f1Tech, Authors[5],
                "Floor edge wear — is porpoising truly gone?",
                "Seeing a bit of bouncing again on some cars at high-speed circuits. Has this actually been solved or just managed?",
                now.AddDays(-13), tagSlugs: ["technical-analysis", "rules-regulations"]);
            AddTopLevel(f1Tech, t, Authors[1], "Managed, not solved — still shows up on bumpier tracks.", now.AddDays(-12));
            var toRemove = AddTopLevel(f1Tech, t, Authors[8], "This thread is a waste of everyone's time honestly.", now.AddDays(-12));
            AddTopLevel(f1Tech, t, Authors[13], "Floor stiffness regs helped a lot compared to the first year of ground effect.", now.AddDays(-11));
            toRemove.SoftDelete(now.AddDays(-11));
            CastVotes(t.Id, VoteTargetType.Thread, t, 15, 1);
        }
        {
            var (t, _) = NewThread(f1Tech, Authors[6],
                "Underrated technical stories of the season",
                "Not everything technical makes headlines. What's a smaller story that deserved more attention?",
                now.AddDays(-1), tagSlugs: ["technical-analysis"]);
            AddTopLevel(f1Tech, t, Authors[2], "The suspension geometry tweaks mid-season went pretty unnoticed.", now.AddHours(-12));
            CastVotes(t.Id, VoteTargetType.Thread, t, 3, 0);
        }

        // === F1 / Driver Discussion =========================================
        var f1Driver = Forum("formula-1", "driver-discussion");
        {
            var (t, _) = NewThread(f1Driver, Authors[10],
                "2027 driver market — who moves where?",
                "Contracts start expiring soon. Who do you think is on the move for 2027?",
                now.AddDays(-20), tagSlugs: ["silly-season", "rumour-mill"]);
            Comment? branch = null;
            for (var i = 0; i < 8; i++)
            {
                var c = AddTopLevel(f1Driver, t, Authors[rng.Next(Authors.Length)],
                    $"Hearing {RandomDriver()} could be a surprise name in the mix.", now.AddDays(-20).AddHours(rng.Next(1, 400)));
                branch ??= c;
            }
            if (branch is not null)
            {
                var r1 = AddReply(f1Driver, t, branch, Authors[3], "Source? That seems unlikely given the contract situation.", now.AddDays(-18));
                AddReply(f1Driver, t, r1, Authors[10], "Just paddock chatter, nothing confirmed yet.", now.AddDays(-17));
            }
            CastVotes(t.Id, VoteTargetType.Thread, t, 45, 6);
        }
        {
            var (t, _) = NewThread(f1Driver, Authors[8],
                "Rate every driver's season so far",
                "Let's do season report cards. I'll start with a few grades in the comments.",
                now.AddDays(-7), tagSlugs: ["fan-content"]);
            for (var i = 0; i < 6; i++)
                AddTopLevel(f1Driver, t, Authors[rng.Next(Authors.Length)], $"{RandomDriver()}: solid B+ season, a couple of costly mistakes.", now.AddDays(-7).AddHours(rng.Next(1, 100)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 19, 2);
        }
        {
            var (t, _) = NewThread(f1Driver, Authors[3],
                "Is it time for a rule on team orders?",
                "Another team order controversy this weekend. Should there be clearer regulation around this?",
                now.AddDays(-10), tagSlugs: ["rules-regulations", "stewards-penalties"]);
            for (var i = 0; i < 5; i++)
                AddTopLevel(f1Driver, t, Authors[rng.Next(Authors.Length)],
                    i % 2 == 0 ? "Teams should be free to manage their own drivers." : "It undermines the sport when the faster car is held up.",
                    now.AddDays(-10).AddHours(rng.Next(1, 200)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 8, 7);
        }
        {
            var (t, _) = NewThread(f1Driver, Authors[4],
                "Rookie of the year candidates",
                "A strong rookie class this year. Who's impressed you most?",
                now.AddDays(-4), tagSlugs: ["fan-content"]);
            AddTopLevel(f1Driver, t, Authors[9], $"{RandomDriver()} has been remarkably consistent for a rookie.", now.AddDays(-3));
            AddTopLevel(f1Driver, t, Authors[12], "Qualifying pace has been the standout for the rookie class this year.", now.AddDays(-2));
            AddTopLevel(f1Driver, t, Authors[1], "Race craft still needs work across the board though.", now.AddDays(-2));
            CastVotes(t.Id, VoteTargetType.Thread, t, 14, 1);
        }
        {
            var (t, _) = NewThread(f1Driver, Authors[6],
                "Throwback: most dominant driver seasons in history",
                "Not counting this year — what's the most dominant single season you've witnessed?",
                now.AddDays(-16), tagSlugs: ["off-topic", "fan-content"]);
            AddTopLevel(f1Driver, t, Authors[0], "2004 still stands out for sheer number of wins.", now.AddDays(-15));
            AddTopLevel(f1Driver, t, Authors[7], "2011 was close to that level of dominance too.", now.AddDays(-14));
            AddTopLevel(f1Driver, t, Authors[13], "2023 deserves a mention for consistency alone.", now.AddDays(-13));
            AddTopLevel(f1Driver, t, Authors[5], "Depends if you weight it by competition faced.", now.AddDays(-12));
            CastVotes(t.Id, VoteTargetType.Thread, t, 21, 3);
        }

        // === WEC / General Discussion ========================================
        var wecGeneral = Forum("wec", "general-discussion");
        {
            var (t, _) = NewThread(wecGeneral, Authors[7],
                "New to WEC — where should I start watching?",
                "Coming over from F1, what's the best entry point into following WEC properly?",
                now.AddDays(-6), tagSlugs: ["off-topic"]);
            AddTopLevel(wecGeneral, t, Authors[9], "Start with last year's Le Mans highlights, hooks everyone.", now.AddDays(-5));
            AddTopLevel(wecGeneral, t, Authors[2], "Learn the class structure first, it's confusing otherwise.", now.AddDays(-5));
            AddTopLevel(wecGeneral, t, Authors[10], "The multi-class racing is what makes it special, stick with it.", now.AddDays(-4));
            AddTopLevel(wecGeneral, t, Authors[4], "Endurance format takes some getting used to but worth it.", now.AddDays(-4));
            CastVotes(t.Id, VoteTargetType.Thread, t, 13, 0);
        }
        {
            var (t, _) = NewThread(wecGeneral, Authors[2],
                "Hypercar class balance of performance discussion",
                "BoP adjustments again this round. Fair, or is it favouring certain manufacturers?",
                now.AddDays(-9), tagSlugs: ["technical-analysis", "rules-regulations"]);
            AddTopLevel(wecGeneral, t, Authors[11], "BoP is always going to be a moving target with this many manufacturers.", now.AddDays(-8));
            AddTopLevel(wecGeneral, t, Authors[6], "Fairly transparent process compared to other series at least.", now.AddDays(-7));
            AddTopLevel(wecGeneral, t, Authors[1], "One manufacturer does seem to benefit more often than the rest.", now.AddDays(-7));
            CastVotes(t.Id, VoteTargetType.Thread, t, 7, 2);
        }
        {
            // Deliberately empty engagement: fresh thread, no comments beyond root, no votes.
            NewThread(wecGeneral, Authors[12],
                "Anyone catching the next round live?",
                "Trying to figure out the local broadcast time for the next round — anyone know if it's streaming this year?",
                now.AddHours(-2), tagSlugs: []);
        }

        // === WEC / Race Discussion ==========================================
        var wecRace = Forum("wec", "race-discussion");
        {
            var (t, _) = NewThread(wecRace, Authors[9],
                "6 Hours of Spa — Race Thread", "Race thread for the 6 Hours of Spa. Post your reactions here.",
                now.AddDays(-45), raceId: "2026-wec-r02-spa", tagSlugs: ["race-strategy"]);
            for (var i = 0; i < 4; i++)
                AddTopLevel(wecRace, t, Authors[rng.Next(Authors.Length)], "Great stint from the leading Hypercar there.", now.AddDays(-45).AddHours(rng.Next(1, 6)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 17, 1);
        }
        {
            var (t, _) = NewThread(wecRace, Authors[11],
                "24 Hours of Le Mans — Race Thread", "The big one. Post your live reactions for all 24 hours here.",
                now.AddDays(-30), raceId: "2026-wec-r03-lemans", tagSlugs: ["race-strategy", "stewards-penalties"]);
            Comment? first = null;
            for (var i = 0; i < 9; i++)
            {
                var c = AddTopLevel(wecRace, t, Authors[rng.Next(Authors.Length)], "Night stints are always where this race is won or lost.", now.AddDays(-30).AddHours(rng.Next(1, 24)));
                first ??= c;
            }
            if (first is not null) AddReply(wecRace, t, first, Authors[2], "The GT class fight was just as good this year too.", now.AddDays(-29));
            CastVotes(t.Id, VoteTargetType.Thread, t, 38, 3);
        }
        {
            var (t, _) = NewThread(wecRace, Authors[5],
                "6 Hours of Fuji — Race Thread", "Race thread for Fuji. How's everyone finding the new layout?",
                now.AddDays(-12), raceId: "2026-wec-r07-fuji", tagSlugs: ["race-strategy"]);
            AddTopLevel(wecRace, t, Authors[0], "Weather looked like it could be a factor again this year.", now.AddDays(-11));
            AddTopLevel(wecRace, t, Authors[8], "LMP2 battle at the front was tighter than the Hypercar fight.", now.AddDays(-11));
            AddTopLevel(wecRace, t, Authors[3], "Solid strategy call from the class leaders in the final hour.", now.AddDays(-10));
            CastVotes(t.Id, VoteTargetType.Thread, t, 12, 2);
        }
        {
            var (t, _) = NewThread(wecRace, Authors[0],
                "8 Hours of Bahrain — Race Thread (season finale)", "Season finale race thread — title fight goes down to the wire.",
                now.AddDays(-5), raceId: "2026-wec-r08-bahrain", tagSlugs: ["race-strategy"]);
            for (var i = 0; i < 5; i++)
                AddTopLevel(wecRace, t, Authors[rng.Next(Authors.Length)], "Title fight coming down to the final stint, incredible season.", now.AddDays(-5).AddHours(rng.Next(1, 8)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 20, 1);
        }

        // === General Motorsport / Paddock Club ==============================
        var paddock = Forum("general-motorsport", "paddock-club");
        {
            var (t, _) = NewThread(paddock, Moderators[0],
                "Welcome to the Paddock Club",
                "This is the off-topic space for anything motorsport-adjacent that doesn't fit the F1/WEC forums. Enjoy!",
                now.AddDays(-60), tagSlugs: ["off-topic"]);
            t.Pin();
            AddTopLevel(paddock, t, Authors[1], "Nice, been wanting a general chat space like this.", now.AddDays(-59));
            AddTopLevel(paddock, t, Authors[7], "Good idea keeping this separate from the race forums.", now.AddDays(-58));
            AddTopLevel(paddock, t, Authors[13], "Following.", now.AddDays(-57));
            CastVotes(t.Id, VoteTargetType.Thread, t, 25, 0);
        }
        {
            var (t, _) = NewThread(paddock, Authors[5],
                "What other motorsport series do you follow?",
                "Besides F1/WEC, what else is on your watchlist? IndyCar, MotoGP, rally, anything?",
                now.AddDays(-14), tagSlugs: ["off-topic", "fan-content"]);
            for (var i = 0; i < 5; i++)
                AddTopLevel(paddock, t, Authors[rng.Next(Authors.Length)], "Been getting into rally lately, underrated series.", now.AddDays(-14).AddHours(rng.Next(1, 200)));
            CastVotes(t.Id, VoteTargetType.Thread, t, 10, 1);
        }
        {
            var (t, _) = NewThread(paddock, Authors[13],
                "Removed: spam thread example",
                "Check out this amazing deal on race merchandise, link in bio, limited time only!!!",
                now.AddDays(-25), tagSlugs: []);
            AddTopLevel(paddock, t, Authors[2], "This looks like spam, reporting it.", now.AddDays(-25));
            CastVotes(t.Id, VoteTargetType.Thread, t, 0, 3);
            var removedAt = now.AddDays(-24);
            AddReport(VoteTargetType.Thread, t.Id, Authors[2], ReportReason.Advertising,
                "Unsolicited advertising, not genuine discussion.", now.AddDays(-25),
                status: ReportStatus.ActionTaken, action: ModerationActionType.ContentRemoved,
                notes: "Confirmed advertising spam, removed.", resolvedBy: Moderators[1], resolvedAt: removedAt);
            SoftDeleteThread(paddock, t, removedAt);
        }

        await db.SaveChangesAsync(ct);

        // === Standalone / unresolved reports (not tied to a removal above) ==
        // Added after the initial save so their target ids are already persisted.
        var teamOrdersThread = allThreads.First(t => t.Title == "Is it time for a rule on team orders?");
        var teamOrdersComment = allComments.First(c => c.ThreadId == teamOrdersThread.Id && !c.IsRoot);
        AddReport(VoteTargetType.Comment, teamOrdersComment.Id, Authors[3], ReportReason.Harassment,
            "Personal attack directed at another user in this comment.", now.AddDays(-9));

        var sillyThread = allThreads.First(t => t.Title == "Silly season mega-thread: 2027 driver line-up predictions");
        var sillyComment = allComments.First(c => c.ThreadId == sillyThread.Id && !c.IsRoot);
        AddReport(VoteTargetType.Comment, sillyComment.Id, Authors[4], ReportReason.Misinformation,
            "Presenting an unconfirmed rumour as fact.", now.AddDays(-6),
            status: ReportStatus.Reviewing, notes: "Looking into the source before deciding.", resolvedBy: Moderators[0]);
        AddReport(VoteTargetType.Thread, sillyThread.Id, Authors[6], ReportReason.Misinformation,
            "Whole thread is speculation being treated as confirmed news.", now.AddDays(-14));

        var controversialThread = allThreads.First(t => t.Title == "Controversial take: current era of F1 is the best era");
        var controversialComment = allComments.First(c => c.ThreadId == controversialThread.Id && !c.IsRoot);
        AddReport(VoteTargetType.Comment, controversialComment.Id, Authors[8], ReportReason.Hate,
            "Comment crosses the line from opinion into personal insults.", now.AddDays(-3));

        var bulkThread = allThreads.First(t => t.RaceId == "2026-r05-chinese");
        var bulkComment = allComments.FirstOrDefault(c => c.ThreadId == bulkThread.Id && !c.IsRoot)
            ?? allComments.First(c => c.Id == bulkThread.RootCommentId);
        AddReport(VoteTargetType.Comment, bulkComment.Id, Authors[10], ReportReason.Spam,
            "Repeated copy-pasted comment across multiple race threads.", now.AddDays(-40));

        var offTopicComment = allComments.First(c => !c.IsRoot);
        AddReport(VoteTargetType.Comment, offTopicComment.Id, Authors[11], ReportReason.Other,
            "Doesn't seem to violate anything, just disagreeing with the OP.", now.AddDays(-8),
            status: ReportStatus.Dismissed, notes: "Not a violation of community guidelines.", resolvedBy: Moderators[0]);

        var driverThread = allThreads.First(t => t.Title == "2027 driver market — who moves where?");
        var driverComment = allComments.First(c => c.ThreadId == driverThread.Id && !c.IsRoot);
        AddReport(VoteTargetType.Comment, driverComment.Id, Authors[12], ReportReason.Harassment,
            "Piling on another user across multiple replies.", now.AddDays(-17),
            status: ReportStatus.ActionTaken, action: ModerationActionType.UserWarned,
            notes: "User warned for personal attacks.", resolvedBy: Moderators[0]);

        var seasonsThread = allThreads.First(t => t.Title == "Throwback: most dominant driver seasons in history");
        var seasonsComment = allComments.First(c => c.ThreadId == seasonsThread.Id && !c.IsRoot);
        AddReport(VoteTargetType.Comment, seasonsComment.Id, Authors[13], ReportReason.Hate,
            "Repeated hateful language directed at a driver's nationality.", now.AddDays(-11),
            status: ReportStatus.ActionTaken, action: ModerationActionType.UserBanned,
            notes: "Repeated hate-speech violations — escalated to UserService for a ban.", resolvedBy: Moderators[1]);

        var bulkThread2 = allThreads.First(t => t.RaceId == "2026-r13-belgian");
        AddReport(VoteTargetType.Thread, bulkThread2.Id, Authors[0], ReportReason.Advertising,
            "Account posting in every race thread with the same promotional link.", now.AddDays(-35),
            status: ReportStatus.Reviewing, notes: "Checking posting history before deciding.", resolvedBy: Moderators[1]);

        await db.SaveChangesAsync(ct);

        // === Backdate created_at (and dependent hot_score) for realism ======
        foreach (var t in allThreads)
        {
            var at = threadCreatedAt[t.Id];
            var hot = Ranking.Hot(t.UpvoteCount, t.DownvoteCount, at);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE threads SET created_at = {at}, hot_score = {hot} WHERE id = {t.Id}", ct);
        }

        foreach (var c in allComments)
        {
            var at = commentCreatedAt[c.Id];
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE comments SET created_at = {at} WHERE id = {c.Id}", ct);
        }

        foreach (var r in allReports)
        {
            var at = reportCreatedAt[r.Id];
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE reports SET created_at = {at} WHERE id = {r.Id}", ct);
        }
    }
}

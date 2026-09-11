namespace Racinglazing.Forum.Domain.Common;

/// <summary>
/// Ranking maths shared by threads and comments. The "hot" formula is the
/// classic Reddit ranking: it balances score against age so that fresh,
/// well-received content floats up and decays over time.
///
/// Storing the result in a column (rather than computing it per query) lets the
/// feed be a plain indexed ORDER BY, which is what keeps reads fast at scale.
/// A background job can periodically re-decay old rows; for the MVP we recompute
/// on every vote, which is sufficient.
/// </summary>
public static class Ranking
{
    // Reddit's epoch. Using a fixed epoch keeps hot scores comparable over time.
    private static readonly DateTimeOffset Epoch = new(2005, 12, 8, 0, 0, 0, TimeSpan.Zero);

    public static double Hot(long upvotes, long downvotes, DateTimeOffset createdAt)
    {
        long score = upvotes - downvotes;
        double order = Math.Log10(Math.Max(Math.Abs(score), 1));
        int sign = score > 0 ? 1 : score < 0 ? -1 : 0;
        double seconds = (createdAt - Epoch).TotalSeconds - 1_134_028_003;
        return Math.Round(sign * order + seconds / 45000.0, 7);
    }

    /// <summary>
    /// "Controversial" ranking: high when up and down votes are both large and
    /// balanced. Declared for later; comment sort exposes it post-MVP.
    /// </summary>
    public static double Controversy(long upvotes, long downvotes)
    {
        if (downvotes <= 0 || upvotes <= 0) return 0;
        long magnitude = upvotes + downvotes;
        double balance = upvotes > downvotes
            ? (double)downvotes / upvotes
            : (double)upvotes / downvotes;
        return Math.Pow(magnitude, balance);
    }
}

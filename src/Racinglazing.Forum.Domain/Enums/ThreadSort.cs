namespace Racinglazing.Forum.Domain.Enums;

/// <summary>
/// Sort orders supported when listing threads. MostCommented / RecentlyUpdated
/// are declared now (extensibility) but only Hot/New/Top are wired for the MVP.
/// </summary>
public enum ThreadSort
{
    Hot = 0,
    New = 1,
    Top = 2,
    MostCommented = 3,
    RecentlyUpdated = 4
}

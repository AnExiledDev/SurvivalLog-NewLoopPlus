using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>
/// The game's endless-only loop rewards that the EndlessRewardsEverywhere setting offers in every mode. The camp rewards
/// and Cartographer stay endless only: they need the endless camps and map. Outside endless the game reads these
/// rewards' strength as 0 in a few places, so the hooks put the game's own endless math back in; this holds that math.
/// </summary>
public static class EndlessRewards
{
    /// <summary>The reward's effect lasts through the first this many days.</summary>
    public const int EarlyDays = 10;

    /// <summary>The game caps the trap cut at 90%.</summary>
    public const double MaxTrapCut = 0.9;

    /// <summary>A wave keeps at least 1% of its health, as in the game.</summary>
    public const double MinWaveHp = 0.01;

    private static readonly HashSet<int> Unlocked = new()
    {
        4001, 4002, 4003,
        4013, 4014, 4015, 4016, 4017, 4019,
        4020, 4021, 4022, 4023, 4024, 4025,
        4026,
    };

    public static IReadOnlyCollection<int> Talents => Unlocked;

    public static bool Unlocks(int talentId) => Unlocked.Contains(talentId);

    /// <summary>Buffer Zone: wave health is cut by the ratio, never below 1%.</summary>
    public static float WaveHp(float hpMultiplier, float ratio) => hpMultiplier * (float)Math.Max(MinWaveHp, 1.0 - ratio);

    /// <summary>Trapcraft: a capture's durability cost is cut by the ratio (at most 90%), rounded as the game rounds, never below 0.</summary>
    public static int TrapCost(int cost, float ratio)
    {
        var cut = Math.Min(Math.Max(ratio, 0f), (float)MaxTrapCut);

        return Math.Max(0, (int)Math.Round(cost * (1f - cut), MidpointRounding.ToEven));
    }

    /// <summary>Will to Survive and Buffer Zone work through days 1 to 10.</summary>
    public static bool IsEarlyDay(int day) => day >= 1 && day <= EarlyDays;

    /// <summary>
    /// The day a nightly settlement pays for. The game moves to the next day before it settles, so the settled day is
    /// the one before.
    /// </summary>
    public static int SettledDay(int dayNow) => dayNow - 1;

    /// <summary>Survival points a ratio pays, rounded as the game rounds; none for a ratio of 0 or less.</summary>
    public static int Points(float ratio) => ratio <= 0 ? 0 : (int)Math.Round(ratio, MidpointRounding.ToEven);

    /// <summary>Seasoned: points for the crises survived since the count was read, none when the count did not rise.</summary>
    public static int CrisisPoints(int survivedBefore, int survivedAfter, float ratio)
    {
        var survived = survivedAfter - survivedBefore;

        return survived <= 0 ? 0 : survived * Points(ratio);
    }
}

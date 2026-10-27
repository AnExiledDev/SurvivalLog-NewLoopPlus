using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>The rules of the New Loop Plus effects that are a single decision each.</summary>
public static class LoopPerks
{
    /// <summary>The most Interest pays in one loop.</summary>
    public const int InterestMax = 100000;

    /// <summary>The share of the points kept that Interest pays again, rounded; never negative and never past <see cref="InterestMax"/>.</summary>
    public static int Interest(int pointsKept, double rate)
    {
        if (pointsKept <= 0 || rate <= 0) return 0;

        return (int)Math.Min(InterestMax, Math.Round(pointsKept * rate, MidpointRounding.AwayFromZero));
    }

    /// <summary>What Safety Net tops a run's end up by: the gap between what it paid and the floor, never below 0.</summary>
    public static int SafetyNet(int settledTotal, double floor) => Math.Max(0, (int)Math.Round(floor) - Math.Max(0, settledTotal));

    /// <summary>
    /// Second Wind turns a death away only for the player's own character, only while the run is still going, only when
    /// Vitality really ran out (other deaths are the story's), and only while uses are left this run.
    /// </summary>
    public static bool TurnsDeathAway(bool isLeader, bool isAlreadyDead, bool isGameOver, double vitality, double usesPerRun, int usesSpent) =>
        isLeader && !isAlreadyDead && !isGameOver && vitality <= 0 && usesSpent < (int)Math.Round(usesPerRun);

    /// <summary>The Vitality Second Wind leaves the player with: at least 1, so the next drain does not end the run at once.</summary>
    public static float VitalityAfterSecondWind(double vitality) => (float)Math.Max(1, Math.Round(vitality));

    /// <summary>A container that gives loot gives one copy more when the roll (0 to 1) lands under the chance. Ground loot never does.</summary>
    public static int LuckyCopies(int copies, bool isGroundLoot, double chance, double roll) =>
        !isGroundLoot && copies > 0 && roll < chance ? copies + 1 : copies;

    /// <summary>
    /// The material Tinkerer gives back from a finished craft: when the roll (0 to 1) lands under the chance, the one the
    /// pick roll (0 to 1) lands on, else none. A craft with no materials gives nothing back.
    /// </summary>
    public static int? TinkererPick(int materials, double chance, double roll, double pickRoll)
    {
        if (materials <= 0 || roll >= chance) return null;

        return Math.Min(materials - 1, Math.Max(0, (int)(pickRoll * materials)));
    }

    /// <summary>
    /// The trade points Scout's Notes reveals: the first <paramref name="count"/> in the game's order whose unlock tag is a
    /// trade point's and is not already held. Cross-trade points and story places are never revealed.
    /// </summary>
    public static IReadOnlyList<string> ScoutedTags(IEnumerable<(int Id, string Tag)> points, Func<string, bool> isHeld, double count)
    {
        const string tradePoint = "TradePointUnlock_";

        return points
            .Where(point => point.Tag != null && point.Tag.StartsWith(tradePoint, StringComparison.Ordinal))
            .OrderBy(point => point.Id)
            .Select(point => point.Tag)
            .Distinct()
            .Where(tag => !isHeld(tag))
            .Take(Math.Max(0, (int)Math.Round(count)))
            .ToList();
    }
}

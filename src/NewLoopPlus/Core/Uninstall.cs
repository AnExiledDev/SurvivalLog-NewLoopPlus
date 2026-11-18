using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>Set the talent to this level; 0 removes it.</summary>
public sealed record TalentRevert(int TalentId, int ToLevel);

/// <summary>The talents to take down and the Planning Points to give back for them.</summary>
public sealed record UninstallSteps(IReadOnlyList<TalentRevert> Reverts, int Refund);

/// <summary>
/// What a save needs before New Loop Plus is removed. Without the mod, a level the game has no row for gives nothing, and
/// the game's reset refunds only levels it has rows for. So every level above the normal game's top level is taken off and
/// paid back, the way the game's reset pays: the current cost of each level taken off.
/// </summary>
public static class Uninstall
{
    /// <param name="owned">Talent ID to the level the player owns.</param>
    /// <param name="gameTopLevel">The highest level the game itself has for a talent; 0 for a talent only the mod adds.</param>
    /// <param name="cost">What a level of a talent costs now.</param>
    public static UninstallSteps Plan(IEnumerable<KeyValuePair<int, int>> owned, Func<int, int> gameTopLevel, Func<int, int, int> cost)
    {
        var reverts = new List<TalentRevert>();
        var refund = 0;

        foreach (var (talentId, level) in owned.OrderBy(pair => pair.Key))
        {
            var top = gameTopLevel(talentId);

            if (level <= top) continue;

            for (var lv = top + 1; lv <= level; lv++) refund += Math.Max(0, cost(talentId, lv));

            reverts.Add(new TalentRevert(talentId, top));
        }

        return new UninstallSteps(reverts, refund);
    }
}

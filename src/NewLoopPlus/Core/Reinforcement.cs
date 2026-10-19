using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>Which door and window buff a talent owner has earned.</summary>
public static class Reinforcement
{
    /// <summary>
    /// The buff from the last rule whose talent buff the owner has, or null when they have none. Rules list a talent's
    /// levels in order, so an owner who somehow holds two levels' buffs still gets only the higher level's reinforcement.
    /// </summary>
    public static int? Pick(IReadOnlyList<ReinforceRule> rules, Func<int, bool> ownerHas)
    {
        for (var i = rules.Count - 1; i >= 0; i--)
        {
            if (ownerHas(rules[i].TalentBuff)) return rules[i].DoorWindowBuff;
        }

        return null;
    }
}

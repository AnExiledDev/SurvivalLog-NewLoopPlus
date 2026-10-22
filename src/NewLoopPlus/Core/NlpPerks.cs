using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>The effects New Loop Plus carries out itself for whoever owns a talent that gives them ("nlp:name").</summary>
public static class NlpEffect
{
    /// <summary>FloatValue0: the share of the Planning Points kept from the last run paid again as the next loop starts.</summary>
    public const string Interest = "interest";

    /// <summary>IntValue0: the fewest Planning Points a run's end can pay; a run that paid less is topped up as the next loop starts.</summary>
    public const string SafetyNet = "safety-net";

    /// <summary>IntValue0: extra end-of-day rewards to choose from.</summary>
    public const string ExtraPick = "extra-pick";

    /// <summary>FloatValue0: the share more Planning Points crises and events reward.</summary>
    public const string CrisisVeteran = "crisis-veteran";

    /// <summary>IntValue0: times per run a death is turned away; FloatValue0: the Vitality left after one.</summary>
    public const string SecondWind = "second-wind";

    /// <summary>FloatValue0: the chance a searched container gives one more copy of its loot.</summary>
    public const string LuckyFinds = "lucky-finds";

    /// <summary>FloatValue0: the share of the last run's skill levels a new run starts with.</summary>
    public const string MuscleMemory = "muscle-memory";

    /// <summary>IntValue0: trade points already found when a run starts.</summary>
    public const string ScoutsNotes = "scouts-notes";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Interest, SafetyNet, ExtraPick, CrisisVeteran, SecondWind, LuckyFinds, MuscleMemory, ScoutsNotes,
    };
}

/// <summary>How much of each New Loop Plus effect the player has, from the talents they own.</summary>
public static class NlpPerks
{
    public const string Whole = "IntValue0";

    public const string Share = "FloatValue0";

    /// <summary>
    /// The sum of <paramref name="field"/> over every talent that gives <paramref name="effect"/>, each read at the level
    /// owned. A talent not owned (level 0) adds nothing, and neither does a level that leaves the field out.
    /// </summary>
    public static double Value(IReadOnlyList<NlpGrant> grants, Func<int, int> ownedLevel, string effect, string field)
    {
        var total = 0.0;

        foreach (var grant in grants.Where(grant => grant.Effect == effect && grant.Levels.Count > 0))
        {
            var lv = ownedLevel(grant.TalentId);

            if (lv <= 0) continue;

            var fields = grant.Levels[Math.Min(lv, grant.Levels.Count) - 1];

            if (fields.LastOrDefault(set => set.Field == field)?.Value is FieldValue.Number number) total += number.Value;
        }

        return total;
    }
}

using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>
/// How much the Planning Points a run earns are scaled. Each part is multiplied by its own setting and by
/// <paramref name="Overall"/>; a negative setting counts as 0.
/// </summary>
public sealed record PointsMultipliers(double Overall, double Survival, double Crisis, double Record)
{
    public static readonly PointsMultipliers None = new(1, 1, 1, 1);

    public bool ChangesAnything => SurvivalFactor != 1 || CrisisFactor != 1 || RecordFactor != 1;

    private double SurvivalFactor => Factor(Survival);

    private double CrisisFactor => Factor(Crisis);

    private double RecordFactor => Factor(Record);

    /// <summary>The game's survival points to Planning Points rate. The game holds it in single precision.</summary>
    public float SurvivalRate(float original) => (float)(original * SurvivalFactor);

    /// <summary>The Planning Points each survived day pays.</summary>
    public int RecordPerDay(int original) => Scaling.Round(original * RecordFactor);

    /// <summary>
    /// A crisis or event reward. Only rewards are scaled; a penalty (0 or less) stays as the game set it.
    /// </summary>
    public int EventReward(int original) => original <= 0 ? original : Scaling.Round(original * CrisisFactor);

    private double Factor(double part) => Math.Max(0, part) * Math.Max(0, Overall);
}

/// <summary>
/// How much loop reward upgrades cost. A talent listed in <paramref name="PerTalent"/> uses its own multiplier in
/// place of <paramref name="Overall"/>.
/// </summary>
public sealed record CostMultipliers(double Overall, IReadOnlyDictionary<int, double> PerTalent)
{
    public static readonly CostMultipliers None = new(1, new Dictionary<int, double>());

    public bool ChangesAnything => Overall != 1 || PerTalent.Count > 0;

    public int Cost(int talentId, int original)
    {
        var multiplier = PerTalent.TryGetValue(talentId, out var own) ? own : Overall;

        return Scaling.Round(original * Math.Max(0, multiplier));
    }
}

public static class Scaling
{
    /// <summary>Points are whole numbers: halves round up, and nothing goes below 0.</summary>
    public static int Round(double value) => Math.Max(0, (int)Math.Round(value, MidpointRounding.AwayFromZero));
}

/// <summary>
/// The game copies some table values into a cache the first time it reads them, and reloads only while the cache is
/// empty (0). An empty cache is left alone, because the game will fill it from the scaled table.
/// </summary>
public static class GameCache
{
    /// <summary>The value to write into the cache, or null when it is empty or already right.</summary>
    public static TValue? Next<TValue>(TValue cached, TValue? scaled)
        where TValue : struct, IEquatable<TValue>
    {
        if (scaled is not { } target || cached.Equals(default(TValue)) || cached.Equals(target)) return null;

        return target;
    }
}

using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>How the game settles a run that ends now. Only a normal run turns into Planning Points on the death screen.</summary>
public enum RunMode
{
    Normal,
    Casual,
    Endless,
    Challenge,
}

/// <summary>
/// The values the game's death settlement reads, taken while the run is still going.
/// <paramref name="SettledStages"/> is the save's record of what an earlier settle already paid this round; it only
/// counts when <paramref name="IsFaithSettled"/> is true.
/// </summary>
public sealed record SettleInputs(
    int SurvivalPoints,
    float SurvivalToBelief,
    bool IsFaithSettled,
    IReadOnlyList<int>? SettledStages,
    int CrisisPoints,
    int Day,
    int RecordRewardPaidDay,
    int RecordRewardBelief);

/// <summary>The Planning Points a death now would pay, in the three parts the game adds up.</summary>
public readonly record struct PointsPreview(int Survival, int Crisis, int Record)
{
    public int Total => Math.Max(0, Survival + Crisis + Record);
}

/// <summary>
/// The game's death settlement, worked out without running it: the real one adds the points and saves, so it may only
/// run when the run really ends. The two paths mirror the game's own: the first settle of a round pays all converted
/// survival points, a later one pays only what was converted since.
/// </summary>
public static class Settlement
{
    /// <summary>The settle record holds the converted survival points first; the game ignores a record shorter than this.</summary>
    private const int SettledStageCount = 3;

    public static PointsPreview Preview(SettleInputs inputs)
    {
        var survival = ConvertedSurvival(inputs);

        if (inputs.IsFaithSettled) survival = Math.Max(0, survival - AlreadyConverted(inputs, survival));

        return new PointsPreview(survival, inputs.CrisisPoints, RecordPart(inputs));
    }

    /// <summary>The game multiplies in single precision, then rounds half to even.</summary>
    private static int ConvertedSurvival(SettleInputs inputs)
    {
        float product = inputs.SurvivalPoints * inputs.SurvivalToBelief;

        return (int)Math.Round((double)product, MidpointRounding.ToEven);
    }

    /// <summary>A missing or short record makes the game treat everything converted so far as already paid.</summary>
    private static int AlreadyConverted(SettleInputs inputs, int converted)
    {
        var stages = inputs.SettledStages;

        return stages is { Count: >= SettledStageCount } ? stages[0] : converted;
    }

    private static int RecordPart(SettleInputs inputs)
    {
        var unpaidDays = inputs.Day - inputs.RecordRewardPaidDay;

        return unpaidDays > 0 ? unpaidDays * inputs.RecordRewardBelief : 0;
    }
}

using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using ExiledToolkit.Game;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Wakes the table checks when the talent levels they read may have changed. A run sets up its talents' buffs from the
/// save in TalentComponent.OnInitBuffs (a new run, a load, a new loop), and TalentComponent.SaveGame puts a fresh copy of
/// the run's talent levels into the save, which is the copy <see cref="Perks"/> reads. If either hook cannot go in, the
/// checks wake every second instead.
/// </summary>
internal static class TableWakes
{
    public static void Apply(PatchKit hooks)
    {
        const string consequence = "the loop reward amounts are checked every second instead";

        var isPlaced = hooks.Patch(typeof(TableWakes), typeof(TalentComponent), nameof(TalentComponent.OnInitBuffs), postfix: nameof(AfterTalents), consequence: consequence);

        isPlaced &= hooks.Patch(typeof(TableWakes), typeof(TalentComponent), nameof(TalentComponent.SaveGame), new[] { typeof(AgentSave) }, postfix: nameof(AfterTalents), consequence: consequence);

        if (!isPlaced) ScaleTicker.IsPolling = true;
    }

    private static void AfterTalents() => ScaleTicker.Wake();
}

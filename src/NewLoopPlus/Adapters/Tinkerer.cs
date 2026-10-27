using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Tinkerer: a finished craft sometimes gives one of its materials back. A workbench finishes a craft in
/// ToolTableManager.TryCreateItem(furnitureId): the bench's ToolTableData.CachedItemIds are the materials put in, and a
/// craft that succeeds removes every one of them before it puts the product on the bench. Before the game runs it, the
/// hook rolls the chance and notes one material; after, if that material is gone (the craft used it), one of the same
/// item goes back onto the bench, as fresh as the one used. A craft that fails leaves the materials, so nothing is given.
/// Static because a Harmony prefix and postfix have nowhere else to look.
/// </summary>
internal static class Tinkerer
{
    private static readonly Random Dice = new();

    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(ToolTableManager), nameof(ToolTableManager.TryCreateItem), new[] { typeof(long) }), prefix: new HarmonyMethod(typeof(Tinkerer), nameof(BeforeCraft)), postfix: new HarmonyMethod(typeof(Tinkerer), nameof(AfterCraft)));
        }
        catch (Exception ex)
        {
            log("could not hook crafting, Tinkerer gives no materials back: " + ex.Message);
        }
    }

    /// <summary>The material noted before a craft, or null when the roll missed. Never throws into the game.</summary>
    private static void BeforeCraft(ToolTableManager __instance, long __0, out Material? __state)
    {
        __state = null;

        try
        {
            var chance = Perks.Share(NlpEffect.Tinkerer);

            if (chance <= 0) return;

            var map = __instance._ToolTableMap_k__BackingField;

            if (map == null || !map.ContainsKey(__0)) return;

            var materials = map[__0]?._CachedItemIds_k__BackingField;
            var items = GameSingleton.Existing<BattleLogicWorld>()?._ItemManager;

            if (materials == null || items == null) return;

            if (LoopPerks.TinkererPick(materials.Count, chance, Dice.NextDouble(), Dice.NextDouble()) is not { } pick) return;

            var id = materials[pick];
            var item = items.GetItemData(id);

            if (item == null) return;

            __state = new Material(id, item._ItemConfigId_k__BackingField, item._TimeLeft_k__BackingField, item._OriginalTimeLeft_k__BackingField, item._UseTimes_k__BackingField, item._MaxUseTimes_k__BackingField);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    /// <summary>Gives the noted material back if the craft used it. Never throws into the game.</summary>
    private static void AfterCraft(long __0, Material? __state)
    {
        if (__state is not { } material) return;

        try
        {
            var world = GameSingleton.Existing<BattleLogicWorld>();
            var items = world?._ItemManager;

            if (world == null || items == null || items.GetItemData(material.InstanceId) != null) return;

            var hour = world._GameTimeManager?.GetHour() ?? 0;

            if (items.TryAddItemToBag(__0, material.ConfigId, 1, hour, material.TimeLeft, material.UseTimes, 1f, null, material.MaxUseTimes, null, null, material.OriginalTimeLeft)) _log($"Tinkerer gave back item {material.ConfigId} from a craft");
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void ReportFailure(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("Tinkerer failed, crafting is the game's own (reported once): " + ex);
    }

    private sealed record Material(long InstanceId, int ConfigId, int TimeLeft, int OriginalTimeLeft, int UseTimes, int MaxUseTimes);
}

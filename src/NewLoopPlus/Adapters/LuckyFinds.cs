using System;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Lucky Finds: a searched container sometimes gives one more copy of its loot. The game rolls how many copies a
/// container's loot comes in with DifficultyTools.RollExploreLootCopies(groundLoot) as it is searched; the hook adds
/// one copy to that roll when the chance comes up. Loot lying on the ground is left as it is.
/// Static because a Harmony postfix has nowhere else to look.
/// </summary>
internal static class LuckyFinds
{
    private static readonly Random Dice = new();

    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(DifficultyTools), nameof(DifficultyTools.RollExploreLootCopies), new[] { typeof(bool) }), postfix: new HarmonyMethod(typeof(LuckyFinds), nameof(AfterRoll)));
        }
        catch (Exception ex)
        {
            log("could not hook loot rolls, Lucky Finds gives no extra loot: " + ex.Message);
        }
    }

    /// <summary>Never throws into the game.</summary>
    private static void AfterRoll(bool __0, ref int __result)
    {
        try
        {
            var chance = Perks.Share(NlpEffect.LuckyFinds);

            if (chance <= 0) return;

            __result = LoopPerks.LuckyCopies(__result, __0, chance, Dice.NextDouble());
        }
        catch (Exception ex)
        {
            if (_hasReportedFailure) return;

            _hasReportedFailure = true;
            _log("Lucky Finds failed, loot is the game's own (reported once): " + ex);
        }
    }
}

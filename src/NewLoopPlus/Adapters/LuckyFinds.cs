using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;
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

    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks)
    {
        _hooks = hooks;

        hooks.Patch(typeof(LuckyFinds), typeof(DifficultyTools), nameof(DifficultyTools.RollExploreLootCopies), new[] { typeof(bool) }, postfix: nameof(AfterRoll),
            consequence: "Lucky Finds gives no extra loot");
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
            _hooks?.Fail("rolling Lucky Finds, loot is the game's own", ex);
        }
    }
}

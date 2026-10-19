using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Pays <see cref="StartingBonus"/> as the loop reward screen opens (Reducer_Web_RebirthUpgrade.RA_Open), before the
/// game reads the run's points for the screen and marks the run as waiting (GameSaveManager.IsMajorRebirthCommitWindow).
/// Static because a Harmony prefix has nowhere else to look.
/// </summary>
internal static class LoopStartBonus
{
    private static Action<string> _log = _ => { };
    private static int _bonus;
    private static bool _hasReportedFailure;

    public static int Bonus
    {
        set => _bonus = Math.Max(0, value);
    }

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(Reducer_Web_RebirthUpgrade), "RA_Open"), prefix: new HarmonyMethod(typeof(LoopStartBonus), nameof(BeforeOpen)));
        }
        catch (Exception ex)
        {
            log("could not hook the loop reward screen, the starting bonus will not be paid: " + ex.Message);
        }
    }

    /// <summary>Never throws into the game, and never stops the screen opening.</summary>
    private static void BeforeOpen(Ac_RebirthUpgrade_Open ac)
    {
        try
        {
            var saves = GameSingleton.Existing<GameSaveManager>();

            if (ac == null || saves == null || StartingBonus.Due(ac.OpenMode, saves.IsMajorRebirthCommitWindow, _bonus) is not { } bonus) return;

            var faith = GameSingleton.Existing<BattleLogicWorld>()?._FaithManager;

            if (faith == null)
            {
                _log("the loop reward screen opened with no run in memory, so the starting bonus was not paid");

                return;
            }

            _log(faith.AddFaithPoint(bonus, "Generic")
                ? $"paid the starting bonus of {bonus} Planning Points for the new loop"
                : $"the game refused the starting bonus of {bonus} Planning Points (a casual save takes none)");
        }
        catch (Exception ex)
        {
            if (_hasReportedFailure) return;

            _hasReportedFailure = true;
            _log("could not pay the starting bonus (reported once): " + ex);
        }
    }
}

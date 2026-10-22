using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Pays <see cref="StartingBonus"/>, Interest and Safety Net as the loop reward screen opens
/// (Reducer_Web_RebirthUpgrade.RA_Open), before the game reads the run's points for the screen and marks the run as
/// waiting (GameSaveManager.IsMajorRebirthCommitWindow). Interest is worked out from the points kept before anything
/// is paid. Safety Net reads what the run's end paid from the save (SaveChildData.FaithSettledTotal) rather than
/// hooking the settlement: SettleFaithPoints has a RIP-relative compare in its first 14 bytes, which Dobby cannot move.
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
            log("could not hook the loop reward screen, the starting bonus, Interest and Safety Net will not be paid: " + ex.Message);
        }
    }

    /// <summary>Never throws into the game, and never stops the screen opening.</summary>
    private static void BeforeOpen(Ac_RebirthUpgrade_Open ac)
    {
        try
        {
            var saves = GameSingleton.Existing<GameSaveManager>();

            if (ac == null || saves == null || !StartingBonus.IsLoopStart(ac.OpenMode, saves.IsMajorRebirthCommitWindow)) return;

            var faith = GameSingleton.Existing<BattleLogicWorld>()?._FaithManager;
            var payments = PaymentsDue(saves, faith);

            if (payments.Count == 0) return;

            if (faith == null)
            {
                _log("the loop reward screen opened with no run in memory, so the starting bonus was not paid");

                return;
            }

            foreach (var payment in payments) Pay(faith, payment);
        }
        catch (Exception ex)
        {
            if (_hasReportedFailure) return;

            _hasReportedFailure = true;
            _log("could not pay the loop start points (reported once): " + ex);
        }
    }

    private static List<Payment> PaymentsDue(GameSaveManager saves, FaithManager? faith)
    {
        var payments = new List<Payment>();
        var interest = faith == null ? 0 : LoopPerks.Interest(faith.GetFaithPoint(), Perks.Share(NlpEffect.Interest));
        var floor = Perks.Whole(NlpEffect.SafetyNet);
        var round = saves.gameSave?.CurSave;
        var safetyNet = floor > 0 && round != null ? LoopPerks.SafetyNet(round.FaithSettledTotal, floor) : 0;

        if (StartingBonus.Due(StartingBonus.AfterRunMode, false, _bonus) is { } bonus) payments.Add(new Payment(bonus, "the starting bonus", Toast: null));

        if (interest > 0) payments.Add(new Payment(interest, "Interest", Texts.InterestPaid));

        if (safetyNet > 0) payments.Add(new Payment(safetyNet, $"Safety Net (the run's end paid {round?.FaithSettledTotal}, the floor is {floor})", Texts.SafetyNetPaid));

        return payments;
    }

    private static void Pay(FaithManager faith, Payment payment)
    {
        if (!faith.AddFaithPoint(payment.Points, "Generic"))
        {
            _log($"the game refused {payment.What} of {payment.Points} Planning Points (a casual save takes none)");

            return;
        }

        _log($"paid {payment.What}: {payment.Points} Planning Points for the new loop");

        if (payment.Toast is { } toast) EndRunTicker.Page?.Toast(toast.Format(GameLanguage.IsChinese(), payment.Points));
    }

    /// <summary>A talent's payment is shown to the player; the starting bonus is a setting they chose, so it is not.</summary>
    private readonly record struct Payment(int Points, string What, Phrase? Toast);
}

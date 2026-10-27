using System;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Gives Mental Toughness levels with more than one use. Using it calls BuffComponent.RemoveBuffLayer, and the game runs
/// the buff's end effects (the morale and Tranquility) only when the last layer goes. With uses left this ends the buff,
/// so the effects run, and starts it again with the uses that are left. Static because Harmony hooks have nowhere else to look.
/// </summary>
internal static class MentalToughnessUses
{
    public const int TalentId = 2010;

    /// <summary>The buffs of Mental Toughness's levels, filled when the talent tables are written.</summary>
    public static MultiUseBuffs Buffs { get; } = new();

    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            var target = AccessTools.Method(typeof(BuffComponent), nameof(BuffComponent.RemoveBuffLayer), new[] { typeof(int), typeof(int) })
                ?? throw new MissingMethodException("BuffComponent.RemoveBuffLayer");

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(MentalToughnessUses), nameof(BeforeRemoveLayer)));
        }
        catch (Exception ex)
        {
            _log($"{HookFailure.Prefix} buff layers, Mental Toughness has one use at every level: " + ex.Message);
        }
    }

    private static bool BeforeRemoveLayer(BuffComponent __instance, int __0, int __1)
    {
        try
        {
            if (Buffs.Count == 0) return true;

            var live = __instance.SearchBuffListByBuffId(__0);
            if (live == null || live.Count == 0) return true;

            var buff = live[0];
            if (buff?.config == null || Buffs.UsesLeft(__0, buff.BuffCount, __1) is not { } left) return true;

            var config = buff.config;
            var releaser = buff.releaserId;

            __instance.RemoveBuff(buff.EffectBtArgsID);
            __instance.PlayBuff(releaser, config, -1f, left, false);
            _log($"Mental Toughness used, {left} left");
            return false;
        }
        catch (Exception ex)
        {
            if (!_hasReportedFailure)
            {
                _hasReportedFailure = true;
                _log("the Mental Toughness hook failed, the game's own step ran instead (reported once): " + ex);
            }

            return true;
        }
    }
}

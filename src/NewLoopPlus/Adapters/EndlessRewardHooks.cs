using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// With EndlessRewardsEverywhere on, the endless rewards a player owns work outside endless as they do in it. Several
/// read their strength only in pure endless (GetEndlessTalentRatio returns 0 elsewhere) or are applied only by endless
/// code, so each hook does the game's endless step itself, and only outside pure endless, where the game already does it:
/// - Greenhouse, Waste Heat: EnvAreaHelper.GetEndlessTalentRatio returns the leader's ratio.
/// - Trapcraft: TrapManager.GetCaptureDurabilityCost gets the endless cut.
/// - Buffer Zone: TowerWaveManager.StartWave cuts wave health through day 10.
/// - Seasoned: DynamicEventManager.ResolveCrisisEvent pays survival points when the survived-crises count rises.
/// - Will to Survive: CountUpTimer.TriggerDaySettlement pays survival points for days 1 to 10.
/// Static because Harmony hooks have nowhere else to look.
/// </summary>
internal static class EndlessRewardHooks
{
    private const string TrapCost = "Buff/AE_EndlessTrapCost";
    private const string EarlyWaveCut = "Buff/AE_EndlessEarlyWaveCut";
    private const string CrisisPoint = "Buff/AE_EndlessCrisisPoint";
    private const string EarlyDailyPoint = "Buff/AE_EndlessEarlyDailyPoint";
    private const string CrisisSurvivedCounter = "crisis.survived";

    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        Hook(harmony, "the endless heat rewards", typeof(EnvAreaHelper), "GetEndlessTalentRatio", new[] { typeof(string) }, postfix: nameof(AfterHeatRatio));
        Hook(harmony, "trap captures, Trapcraft", typeof(TrapManager), "GetCaptureDurabilityCost", new[] { typeof(Config_Trap) }, postfix: nameof(AfterTrapCost));
        Hook(harmony, "waves, Buffer Zone", typeof(TowerWaveManager), "StartWave", new[] { typeof(TowerWaveManager.WaveRuntimeConfig), typeof(string) }, prefix: nameof(BeforeWave));
        Hook(harmony, "crises, Seasoned", typeof(DynamicEventManager), nameof(DynamicEventManager.ResolveCrisisEvent), new[] { typeof(int) }, prefix: nameof(BeforeCrisis), postfix: nameof(AfterCrisis));
        Hook(harmony, "the nightly settlement, Will to Survive", typeof(CountUpTimer), "TriggerDaySettlement", Type.EmptyTypes, postfix: nameof(AfterDaySettlement));
    }

    private static void Hook(Harmony harmony, string what, Type type, string method, Type[] args, string? prefix = null, string? postfix = null)
    {
        try
        {
            var target = AccessTools.Method(type, method, args) ?? throw new MissingMethodException(type.Name + "." + method);

            harmony.Patch(target,
                prefix: prefix == null ? null : new HarmonyMethod(typeof(EndlessRewardHooks), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(EndlessRewardHooks), postfix));
        }
        catch (Exception ex)
        {
            _log($"{HookFailure.Prefix} {what}, it works only in endless: " + ex.Message);
        }
    }

    /// <summary>On only when the setting is on and the game is not in pure endless, where it does all this itself.</summary>
    private static bool IsActive => EndlessRewardsSetting.On && !GameKey.IsPureEndlessActive;

    /// <summary>The leader's strength of a reward's effect, 0 when none is owned or nothing is loaded.</summary>
    private static float Ratio(string effect)
    {
        var leader = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();
        var buffs = leader == null ? null : AgentTools.GetAgentComponent<BuffComponent>(leader);

        return buffs?.GetTalentEffectRatio(effect) ?? 0f;
    }

    private static void AfterHeatRatio(string __0, ref float __result)
    {
        try
        {
            if (__result == 0f && IsActive) __result = Ratio(__0);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void AfterTrapCost(ref int __result)
    {
        try
        {
            if (IsActive) __result = EndlessRewards.TrapCost(__result, Ratio(TrapCost));
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void BeforeWave(TowerWaveManager.WaveRuntimeConfig __0)
    {
        try
        {
            if (__0 == null || !IsActive) return;

            var day = GameSingleton.Existing<BattleLogicWorld>()?._GameTimeManager?.GetDay() ?? 0;

            if (!EndlessRewards.IsEarlyDay(day)) return;

            var ratio = Ratio(EarlyWaveCut);

            if (ratio > 0f) __0.HpMultiplier = EndlessRewards.WaveHp(__0.HpMultiplier, ratio);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void BeforeCrisis(out int __state)
    {
        __state = -1;

        try
        {
            if (IsActive) __state = GameCounter.GetCounter(CrisisSurvivedCounter);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void AfterCrisis(int __state)
    {
        if (__state < 0) return;

        try
        {
            var points = EndlessRewards.CrisisPoints(__state, GameCounter.GetCounter(CrisisSurvivedCounter), Ratio(CrisisPoint));

            if (points > 0) Pay(points, "Seasoned");
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void AfterDaySettlement(CountUpTimer __instance)
    {
        try
        {
            if (!IsActive || !EndlessRewards.IsEarlyDay(EndlessRewards.SettledDay(__instance._Day_k__BackingField))) return;

            var points = EndlessRewards.Points(Ratio(EarlyDailyPoint));

            if (points > 0) Pay(points, "Will to Survive");
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void Pay(int points, string reward)
    {
        GameSingleton.Existing<BattleLogicWorld>()?._SurvivalResultsManager?.AddSurvivalPoint(points);
        _log($"{reward} paid {points} survival points");
    }

    private static void ReportFailure(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("an endless reward hook failed, that reward works only in endless (reported once): " + ex);
    }
}

/// <summary>The EndlessRewardsEverywhere setting, read at start: the talent tables are written once, so it applies at restart.</summary>
internal static class EndlessRewardsSetting
{
    public static bool On { get; set; }
}

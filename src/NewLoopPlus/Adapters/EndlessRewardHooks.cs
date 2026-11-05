using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
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
    private const string Doing = "running an endless reward outside endless, that reward works only in endless";

    private static Action<string> _log = _ => { };
    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks, Action<string> log)
    {
        _log = log;
        _hooks = hooks;

        Hook(hooks, typeof(EnvAreaHelper), "GetEndlessTalentRatio", new[] { typeof(string) }, "the endless heat rewards work only in endless", postfix: nameof(AfterHeatRatio));
        Hook(hooks, typeof(TrapManager), "GetCaptureDurabilityCost", new[] { typeof(Config_Trap) }, "Trapcraft works only in endless", postfix: nameof(AfterTrapCost));
        Hook(hooks, typeof(TowerWaveManager), "StartWave", new[] { typeof(TowerWaveManager.WaveRuntimeConfig), typeof(string) }, "Buffer Zone works only in endless", prefix: nameof(BeforeWave));
        Hook(hooks, typeof(DynamicEventManager), nameof(DynamicEventManager.ResolveCrisisEvent), new[] { typeof(int) }, "Seasoned works only in endless", prefix: nameof(BeforeCrisis), postfix: nameof(AfterCrisis));
        Hook(hooks, typeof(CountUpTimer), "TriggerDaySettlement", Type.EmptyTypes, "Will to Survive works only in endless", postfix: nameof(AfterDaySettlement));
    }

    private static void Hook(PatchKit hooks, Type type, string method, Type[] args, string consequence, string? prefix = null, string? postfix = null) =>
        hooks.Patch(typeof(EndlessRewardHooks), type, method, args, prefix, postfix, consequence: consequence);

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
            _hooks?.Fail(Doing, ex);
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
            _hooks?.Fail(Doing, ex);
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
            _hooks?.Fail(Doing, ex);
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
            _hooks?.Fail(Doing, ex);
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
            _hooks?.Fail(Doing, ex);
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
            _hooks?.Fail(Doing, ex);
        }
    }

    private static void Pay(int points, string reward)
    {
        GameSingleton.Existing<BattleLogicWorld>()?._SurvivalResultsManager?.AddSurvivalPoint(points);
        _log($"{reward} paid {points} survival points");
    }
}

/// <summary>The EndlessRewardsEverywhere setting, read at start: the talent tables are written once, so it applies at restart.</summary>
internal static class EndlessRewardsSetting
{
    public static bool On { get; set; }
}

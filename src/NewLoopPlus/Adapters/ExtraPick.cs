using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;
using Il2CppIntSet = Il2CppSystem.Collections.Generic.ICollection<int>;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Extra Pick: more end-of-day rewards to choose from. The game picks them in SurvivalPlanningComponent's
/// GetRandomSurvivalPlanIds, one overload when a day ends and one on a refresh; both draw from the day's
/// Config_DailyRandom groups and cut the list to 3, and a day with a fixed list (no groups) is left alone. The hooks
/// run after the game's pick and draw the extras from the same groups the same way, keeping out every reward already
/// offered and the ones the game is cooling down. They run last, so with More Rewards installed the extras come on top
/// of its count. Buying does not check the offer list, so an extra reward is bought like the game's own.
/// Static because Harmony hooks are.
/// </summary>
internal static class ExtraPick
{
    private static readonly Type[] DayParameters = { typeof(int), typeof(Il2CppIntSet), typeof(int), typeof(int) };
    private static readonly Type[] RefreshParameters = { typeof(int), typeof(Il2CppInts), typeof(Il2CppInts), typeof(Il2CppIntSet), typeof(int), typeof(int) };
    private static readonly HashSet<string> ReportedFailures = new();

    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        Hook(harmony, DayParameters, nameof(AddToDayRewards), "the end of the day");
        Hook(harmony, RefreshParameters, nameof(AddToRefresh), "a refresh");
    }

    private static void Hook(Harmony harmony, Type[] parameters, string postfix, string when)
    {
        try
        {
            var target = AccessTools.Method(typeof(SurvivalPlanningComponent), nameof(SurvivalPlanningComponent.GetRandomSurvivalPlanIds), parameters);

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(ExtraPick), postfix) { priority = Priority.Last });
        }
        catch (Exception ex)
        {
            _log($"could not hook the end-of-day rewards, Extra Pick adds nothing to {when}: {ex.Message}");
        }
    }

    private static void AddToDayRewards(SurvivalPlanningComponent __instance, int day, Il2CppIntSet cooldownIds, Il2CppInts __result)
    {
        try
        {
            RewardDraw draw = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, ToGame(offered), cooldownIds));

            TopUp(__instance, day, __result, "end of day", (offered, target, groups) => OfferFill.TopUp(offered, target, groups, draw));
        }
        catch (Exception ex)
        {
            Fail("adding Extra Pick rewards at the end of the day", ex);
        }
    }

    /// <summary>A refresh draws rewards not shown before, falling back to ones shown before only when those run out.</summary>
    private static void AddToRefresh(SurvivalPlanningComponent __instance, int day, Il2CppInts displayedIds, Il2CppInts buyedIds, Il2CppIntSet cooldownIds, Il2CppInts __result)
    {
        try
        {
            RewardDraw unseen = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, displayedIds, buyedIds, ToGame(offered), cooldownIds));
            RewardDraw seenBefore = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, null, buyedIds, ToGame(offered), cooldownIds));

            TopUp(__instance, day, __result, "refresh", (offered, target, groups) => OfferFill.TopUpRefresh(offered, target, groups, unseen, seenBefore));
        }
        catch (Exception ex)
        {
            Fail("adding Extra Pick rewards on a refresh", ex);
        }
    }

    private static void TopUp(SurvivalPlanningComponent planning, int day, Il2CppInts offered, string when, Func<IReadOnlyList<int>, int, IReadOnlyList<RewardGroup>, IReadOnlyList<int>> fill)
    {
        var extra = Perks.Whole(NlpEffect.ExtraPick);

        if (extra <= 0 || planning == null || offered == null || DayGroups(day) is not { } groups) return;

        var target = offered.Count + extra;
        var added = fill(FromGame(offered), target, groups);

        foreach (var reward in added) offered.Add(reward);

        _log($"day {day} {when}: Extra Pick added {added.Count} of {extra} rewards");
    }

    /// <summary>The day's reward groups, or null on a day the game gives a fixed list.</summary>
    private static IReadOnlyList<RewardGroup>? DayGroups(int day)
    {
        var config = GameSingleton.Existing<ConfigManager>()?.GetDailyRandomConfig(day);
        var ids = config?.RandomPlanID;
        var amounts = config?.RandomAmount;

        if (ids == null || amounts == null || ids.Count == 0 || amounts.Count == 0) return null;

        var groups = new List<RewardGroup>();

        for (var i = 0; i < ids.Count && i < amounts.Count; i++) groups.Add(new RewardGroup(ids[i], amounts[i]));

        return groups;
    }

    /// <summary>Each failure is written once: the game calls these hooks again and again.</summary>
    private static void Fail(string doing, Exception ex)
    {
        if (ReportedFailures.Add(doing)) _log($"failed {doing} (reported once): {ex}");
    }

    private static int? First(Il2CppInts? rewards) => rewards != null && rewards.Count > 0 ? rewards[0] : null;

    private static List<int> FromGame(Il2CppInts rewards)
    {
        var list = new List<int>(rewards.Count);

        for (var i = 0; i < rewards.Count; i++) list.Add(rewards[i]);

        return list;
    }

    private static Il2CppInts ToGame(IReadOnlyList<int> rewards)
    {
        var list = new Il2CppInts();

        foreach (var reward in rewards) list.Add(reward);

        return list;
    }
}

using System;
using System.Collections.Generic;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// A talent level hands out one buff, so a reward that gives several things hands out the first and this adds the rest
/// alongside it: whenever anything gets a level buff, it gets that level's companion buffs from the same releaser, and
/// when the level buff is removed, so are they. Static because a Harmony postfix has nowhere else to look.
/// </summary>
internal static class CompanionBuffs
{
    private static readonly Dictionary<int, IReadOnlyList<int>> Riders = new();
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(BuffComponent), nameof(BuffComponent.AddBuff), new[] { typeof(long), typeof(int) }), postfix: new HarmonyMethod(typeof(CompanionBuffs), nameof(AfterAdd)));
            harmony.Patch(AccessTools.Method(typeof(BuffComponent), nameof(BuffComponent.RemoveBuff), new[] { typeof(int) }), postfix: new HarmonyMethod(typeof(CompanionBuffs), nameof(AfterRemove)));
        }
        catch (Exception ex)
        {
            log("could not hook buffs, a loop reward that gives more than one thing gives only its first: " + ex.Message);
        }
    }

    /// <summary>Replaces the companions; called each time the talents file is written into the tables.</summary>
    public static void Use(IReadOnlyList<Companion> companions)
    {
        Riders.Clear();

        foreach (var companion in companions) Riders[companion.LevelBuff] = companion.Buffs;
    }

    /// <summary>Never throws into the game. A companion is never a level buff, so adding one does not cascade.</summary>
    private static void AfterAdd(BuffComponent __instance, long __0, int __1)
    {
        if (!Riders.TryGetValue(__1, out var riders)) return;

        try
        {
            foreach (var rider in riders) __instance.AddBuff(__0, rider);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    private static void AfterRemove(BuffComponent __instance, int __0)
    {
        if (!Riders.TryGetValue(__0, out var riders)) return;

        try
        {
            foreach (var rider in riders) __instance.RemoveBuff(rider);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    private static void ReportOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("could not add or remove a loop reward's extra buffs (reported once): " + ex);
    }
}

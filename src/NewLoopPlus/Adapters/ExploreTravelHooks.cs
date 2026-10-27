using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Max exposure and pre-disaster travel time. The most exposure a trip out can take is the map's own limit plus
/// ProficiencyManager.GetExploreMaxExposureBonus (the Exploring skill's bonus), so the bonus gets the reward on top.
/// Travel time before the disaster is multiplied by VehicleConfig.GetEffectiveTimeCoeff (1 with no vehicle), so that
/// multiplier gets the cut, with or without a vehicle. The hooks go in only once a level is owned and return at once
/// when the amount is 0. Static because a Harmony postfix has nowhere else to look.
/// </summary>
internal static class ExploreTravelHooks
{
    private static Harmony? _harmony;
    private static Action<string> _log = _ => { };
    private static bool _exposureHooked;
    private static bool _travelHooked;

    /// <summary>Exposure added to the game's bonus; set by PerkTables.</summary>
    public static int ExposureAdded { get; private set; }

    /// <summary>The share cut from travel time; set by PerkTables.</summary>
    public static double TravelCut { get; private set; }

    public static void Prepare(Harmony harmony, Action<string> log)
    {
        _harmony = harmony;
        _log = log;
    }

    public static void Use(int exposureAdded, double travelCut)
    {
        ExposureAdded = exposureAdded;
        TravelCut = travelCut;

        if (_harmony == null) return;

        if (exposureAdded > 0 && !_exposureHooked)
        {
            _exposureHooked = true;
            Hook(AccessTools.Method(typeof(ProficiencyManager), nameof(ProficiencyManager.GetExploreMaxExposureBonus)), nameof(AfterExposureBonus), "max exposure, the max exposure reward adds nothing");
        }

        if (travelCut > 0 && !_travelHooked)
        {
            _travelHooked = true;
            Hook(AccessTools.Method(typeof(VehicleConfig), nameof(VehicleConfig.GetEffectiveTimeCoeff)), nameof(AfterTimeCoeff), "travel time, the travel time reward cuts nothing");
        }
    }

    private static void Hook(System.Reflection.MethodBase target, string postfix, string what)
    {
        try
        {
            _harmony!.Patch(target, postfix: new HarmonyMethod(typeof(ExploreTravelHooks), postfix));
        }
        catch (Exception ex)
        {
            _log($"could not hook {what}: {ex.Message}");
        }
    }

    private static void AfterExposureBonus(ref int __result)
    {
        if (ExposureAdded <= 0) return;

        __result = PerkSettings.ExposureBonus(__result, ExposureAdded);
    }

    private static void AfterTimeCoeff(ref float __result)
    {
        if (TravelCut <= 0) return;

        __result = PerkSettings.TravelCoeff(__result, TravelCut);
    }
}

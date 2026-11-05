using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
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
    private static PatchKit? _hooks;
    private static bool _exposureHooked;
    private static bool _travelHooked;

    /// <summary>Exposure added to the game's bonus; set by PerkTables.</summary>
    public static int ExposureAdded { get; private set; }

    /// <summary>The share cut from travel time; set by PerkTables.</summary>
    public static double TravelCut { get; private set; }

    public static void Prepare(PatchKit hooks)
    {
        _hooks = hooks;
    }

    public static void Use(int exposureAdded, double travelCut)
    {
        ExposureAdded = exposureAdded;
        TravelCut = travelCut;

        if (_hooks == null) return;

        if (exposureAdded > 0 && !_exposureHooked)
        {
            _exposureHooked = true;
            _hooks.Patch(typeof(ExploreTravelHooks), typeof(ProficiencyManager), nameof(ProficiencyManager.GetExploreMaxExposureBonus), postfix: nameof(AfterExposureBonus),
                consequence: "the max exposure reward adds nothing");
        }

        if (travelCut > 0 && !_travelHooked)
        {
            _travelHooked = true;
            _hooks.Patch(typeof(ExploreTravelHooks), typeof(VehicleConfig), nameof(VehicleConfig.GetEffectiveTimeCoeff), postfix: nameof(AfterTimeCoeff),
                consequence: "the travel time reward cuts nothing");
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

using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Show;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Keeps a drone on short trips visible (<see cref="DroneShow"/>). AgentModel.PlayDroneTimeline(which) plays a drone
/// animation; _droneTimelineWhich is the one playing, cleared when it stops. Before a return, a departure still
/// playing is forgotten, so the stop the return causes does not hide the drone. Drone Pilot and Fast Rotors make trips
/// short enough for this. Static because a Harmony prefix has nowhere else to look.
/// </summary>
internal static class DroneReturnShow
{
    private static PatchKit? _hooks;

    public static void Apply(PatchKit hooks)
    {
        _hooks = hooks;

        hooks.Patch(typeof(DroneReturnShow), typeof(AgentModel), nameof(AgentModel.PlayDroneTimeline), new[] { typeof(string) }, prefix: nameof(BeforePlay),
            consequence: "a drone back from a very short trip can stay invisible");
    }

    /// <summary>Never throws into the game.</summary>
    private static void BeforePlay(AgentModel __instance, string __0)
    {
        try
        {
            if (DroneShow.ForgetsDeparture(__0, __instance._droneTimelineWhich)) __instance._droneTimelineWhich = null;
        }
        catch (Exception ex)
        {
            _hooks?.Fail("keeping a drone visible after a short trip", ex);
        }
    }
}

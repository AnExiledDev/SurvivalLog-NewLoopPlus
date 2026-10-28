namespace NewLoopPlus.Core;

/// <summary>
/// The drone hub's drone flies off with its "Dispatch" animation and back with its "Return" one. When the departure ends
/// on its own, the drone is hidden until the return shows it again. A trip shorter than the departure animation asks for
/// the return while the departure still plays: the return shows the drone and stops the departure, and the game then
/// treats that stop as a finished departure and hides the drone again, for good. Forgetting the departure first leaves
/// its stop with nothing to hide.
/// </summary>
public static class DroneShow
{
    public const string Dispatch = "Dispatch";
    public const string Return = "Return";

    /// <param name="asked">The animation the game is about to play.</param>
    /// <param name="playing">The animation the drone is in the middle of, or null when none is.</param>
    public static bool ForgetsDeparture(string? asked, string? playing) => asked == Return && playing == Dispatch;
}

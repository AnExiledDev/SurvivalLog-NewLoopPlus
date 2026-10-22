namespace NewLoopPlus.Core;

/// <summary>
/// Extra Planning Points for each new loop, paid as the loop reward screen opens after a run, so they can be spent on
/// that screen. The screen opens that way once per run: the game marks the run as waiting for its loop rewards to be
/// confirmed, and a save left in that state (the game closed on the screen) is not written, so the run's points come
/// back without the bonus and it is paid again.
/// </summary>
public static class StartingBonus
{
    /// <summary>The loop reward screen after a run; other modes show it for endless records.</summary>
    public const int AfterRunMode = 0;

    public const int Max = 100000;

    /// <summary>True when the screen opens to start a new loop, the one time per run anything is paid there.</summary>
    public static bool IsLoopStart(int openMode, bool isAlreadyWaiting) => openMode == AfterRunMode && !isAlreadyWaiting;

    /// <summary>The points to pay as the screen opens, or null when none are due.</summary>
    public static int? Due(int openMode, bool isAlreadyWaiting, int bonus)
    {
        if (!IsLoopStart(openMode, isAlreadyWaiting) || bonus <= 0) return null;

        return System.Math.Min(bonus, Max);
    }
}

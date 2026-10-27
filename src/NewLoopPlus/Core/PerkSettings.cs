using System;

namespace NewLoopPlus.Core;

/// <summary>
/// The game settings Family Recipe and Drone Pilot change, worked out from the game's own value and the amount owned.
/// With nothing owned every value is the game's own.
/// </summary>
public static class PerkSettings
{
    /// <summary>The shortest a trade drone trip gets, however much Drone Pilot cuts.</summary>
    public const int ShortestTripSeconds = 60;

    /// <summary>A trade drone trip with <paramref name="cutSeconds"/> taken off, never under a minute (or the game's own, when that is shorter).</summary>
    public static int TripSeconds(int gameSeconds, double cutSeconds)
    {
        var cut = (int)Math.Round(cutSeconds);

        if (cut <= 0 || gameSeconds <= ShortestTripSeconds) return gameSeconds;

        return Math.Max(ShortestTripSeconds, gameSeconds - cut);
    }

    /// <summary>A cooking quality bonus with <paramref name="added"/> points on top.</summary>
    public static int CookingBonus(int gameBonus, double added) => gameBonus + Math.Max(0, (int)Math.Round(added));

    /// <summary>The Cooking level multi-pot cooking unlocks at, <paramref name="levelsSooner"/> earlier, never below 1.</summary>
    public static float MultiPotLevel(float gameLevel, double levelsSooner)
    {
        var sooner = (int)Math.Round(levelsSooner);

        if (sooner <= 0 || gameLevel <= 1) return gameLevel;

        return Math.Max(1f, gameLevel - sooner);
    }
}

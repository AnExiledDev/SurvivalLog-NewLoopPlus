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

    /// <summary>The most a discount takes off a vehicle's price.</summary>
    public const double MaxVehicleDiscount = 0.9;

    /// <summary>The most travel time gets cut.</summary>
    public const double MaxTravelCut = 0.9;

    /// <summary>The affinity a basket gives, with <paramref name="added"/> on top.</summary>
    public static int AffinityPerBasket(int gameAffinity, double added) => gameAffinity + Math.Max(0, (int)Math.Round(added));

    /// <summary>A vehicle's price with <paramref name="discount"/> taken off, rounded, never below 1 (or the game's own, when that is lower).</summary>
    public static int VehiclePrice(int gamePrice, double discount)
    {
        var cut = Math.Min(Math.Max(discount, 0), MaxVehicleDiscount);

        if (cut <= 0 || gamePrice <= 1) return gamePrice;

        return Math.Max(1, (int)Math.Round(gamePrice * (1 - cut), MidpointRounding.AwayFromZero));
    }

    /// <summary>The travel time multiplier with <paramref name="cut"/> taken off.</summary>
    public static float TravelCoeff(float gameCoeff, double cut)
    {
        var share = Math.Min(Math.Max(cut, 0), MaxTravelCut);

        return share <= 0 ? gameCoeff : (float)(gameCoeff * (1 - share));
    }

    /// <summary>The game's max exposure bonus with <paramref name="added"/> on top.</summary>
    public static int ExposureBonus(int gameBonus, double added) => gameBonus + Math.Max(0, (int)Math.Round(added));
}

using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Family Recipe and Drone Pilot change the game's settings, so cooking, the trade drone and their previews all
/// show the change with nothing patched. Family Recipe adds to GlobalSetting CookingQuality_FreshBonus and
/// CookingQuality_ExactMatchBonus and lowers CookingMultiPot_UnlockLevel; Drone Pilot shortens
/// StrangerTradeDroneRoundtripSec; Good Neighbor adds to Neighbor_AffinitySend and Companion_AffinitySend (the affinity
/// a basket gives); the vehicle discount lowers each Config_Vehicle Price, which the shop, its refund and the purchase
/// all read. The GameKey caches of the settings are kept in step too. Max exposure and travel time are hooks
/// (<see cref="ExploreTravelHooks"/>), told the amounts here, and so are the phone's loan choices (<see cref="LoanTierHooks"/>). The amounts follow the levels
/// owned, so each check works them out again and, when they changed, every value is worked out again from the game's.
/// </summary>
internal sealed class PerkTables
{
    private const float CheckSeconds = 1f;

    private readonly Action<string> _log;
    private readonly ScaledValues<string, int> _whole;
    private readonly ScaledValues<string, float> _multiPot;
    private readonly ScaledValues<int, int> _vehiclePrice;
    private Amounts _amounts = default;
    private float _nextCheckAt;

    public PerkTables(Action<string> log)
    {
        _log = log;
        _whole = new ScaledValues<string, int>(ScaleWhole);
        _vehiclePrice = new ScaledValues<int, int>((_, price) => PerkSettings.VehiclePrice(price, _amounts.VehicleDiscount));
        _multiPot = new ScaledValues<string, float>((_, level) => PerkSettings.MultiPotLevel(level, _amounts.LevelsSooner));
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        LoanTierHooks.Use(Perks.Level(LoanTierHooks.TalentId));

        // With neither effect offered and nothing written, the game's values stand and there is nothing to check.
        if (!IsAnyOffered() && _amounts == default) return;

        var amounts = new Amounts(Perks.Whole(NlpEffect.DronePilot), Perks.Whole(NlpEffect.FamilyRecipe), (int)Math.Round(Perks.Value(NlpEffect.FamilyRecipe, NlpPerks.SecondWhole)),
            Perks.Whole(NlpEffect.GoodNeighbor), Math.Round(Perks.Share(NlpEffect.VehicleDiscount), 4), Perks.Whole(NlpEffect.MaxExposure), Math.Round(Perks.Share(NlpEffect.TravelTime), 4), Math.Round(Perks.Share(NlpEffect.DroneSpeed), 4));

        if (amounts != _amounts)
        {
            _amounts = amounts;
            _whole.Rescale();
            _multiPot.Rescale();
            _vehiclePrice.Rescale();
            ExploreTravelHooks.Use(amounts.Exposure, amounts.TravelCut);
            _log($"loop reward amounts: {amounts}");
        }

        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null) return;

        if (Whole(config, GameKey.Key_GlobalSetting_StrangerTradeDroneRoundtripSec, "a drone trip now takes {0} seconds (game: {1})") is { } trip && GameCache.Next(GameKey._GlobalSetting_StrangerTradeDroneRoundtripSec, trip) is { } cachedTrip) GameKey._GlobalSetting_StrangerTradeDroneRoundtripSec = cachedTrip;

        if (Whole(config, GameKey.Key_GlobalSetting_CookingQuality_FreshBonus, "fresh ingredients now add {0} cooking quality (game: {1})") is { } fresh && GameCache.Next(GameKey._GlobalSetting_CookingQuality_FreshBonus, fresh) is { } cachedFresh) GameKey._GlobalSetting_CookingQuality_FreshBonus = cachedFresh;

        if (Whole(config, GameKey.Key_GlobalSetting_CookingQuality_ExactMatchBonus, "an exact recipe now adds {0} cooking quality (game: {1})") is { } exact && GameCache.Next(GameKey._GlobalSetting_CookingQuality_ExactMatchBonus, exact) is { } cachedExact) GameKey._GlobalSetting_CookingQuality_ExactMatchBonus = cachedExact;

        if (Whole(config, GameKey.Key_GlobalSetting_Neighbor_AffinitySend, "a basket now gives the neighbor {0} affinity (game: {1})") is { } neighbor && GameCache.Next(GameKey._GlobalSetting_Neighbor_AffinitySend, neighbor) is { } cachedNeighbor) GameKey._GlobalSetting_Neighbor_AffinitySend = cachedNeighbor;

        if (Whole(config, GameKey.Key_GlobalSetting_Companion_AffinitySend, "a basket now gives the companion {0} affinity (game: {1})") is { } companion && GameCache.Next(GameKey._GlobalSetting_Companion_AffinitySend, companion) is { } cachedCompanion) GameKey._GlobalSetting_Companion_AffinitySend = cachedCompanion;

        VehiclePrices(config);

        if (MultiPot(config) is { } level && GameCache.Next(GameKey._GlobalSetting_CookingMultiPot_UnlockLevel, level) is { } cachedLevel) GameKey._GlobalSetting_CookingMultiPot_UnlockLevel = cachedLevel;
    }

    /// <summary>Writes a Params1 setting; returns the value it should hold, or null before the table is loaded.</summary>
    private int? Whole(ConfigManager config, string? key, string written)
    {
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return null;

        if (_whole.Next(key, entry._Params1_k__BackingField) is { } value)
        {
            entry._Params1_k__BackingField = value;
            Reported.Scaled(_whole, key, nameof(Config_GlobalSetting), key, nameof(Config_GlobalSetting.Params1));
            _log(string.Format(written, value, _whole.Original(key)));
        }

        return _whole.Scaled(key);
    }

    private float? MultiPot(ConfigManager config)
    {
        var key = GameKey.Key_GlobalSetting_CookingMultiPot_UnlockLevel;
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return null;

        if (_multiPot.Next(key, entry._Params2_k__BackingField) is { } level)
        {
            entry._Params2_k__BackingField = level;
            Reported.Scaled(_multiPot, key, nameof(Config_GlobalSetting), key, nameof(Config_GlobalSetting.Params2));
            _log($"multi-pot cooking now unlocks at Cooking {level:0} (game: {_multiPot.Original(key):0})");
        }

        return _multiPot.Scaled(key);
    }

    private static bool IsAnyOffered() =>
        Perks.IsOffered(NlpEffect.FamilyRecipe) || Perks.IsOffered(NlpEffect.DronePilot) || Perks.IsOffered(NlpEffect.GoodNeighbor) || Perks.IsOffered(NlpEffect.VehicleDiscount) || Perks.IsOffered(NlpEffect.MaxExposure) || Perks.IsOffered(NlpEffect.TravelTime) || Perks.IsOffered(NlpEffect.DroneSpeed);

    private int ScaleWhole(string key, int value)
    {
        if (key == GameKey.Key_GlobalSetting_StrangerTradeDroneRoundtripSec) return PerkSettings.TripSeconds(value, _amounts.TripCut, _amounts.DroneCut);

        if (key == GameKey.Key_GlobalSetting_Neighbor_AffinitySend || key == GameKey.Key_GlobalSetting_Companion_AffinitySend) return PerkSettings.AffinityPerBasket(value, _amounts.Affinity);

        return PerkSettings.CookingBonus(value, _amounts.CookingBonus);
    }

    /// <summary>Writes each vehicle's price. Three rows, checked once a second.</summary>
    private void VehiclePrices(ConfigManager config)
    {
        var table = config._Config_Vehicle_Dict;

        if (table == null) return;

        foreach (var pair in table)
        {
            var row = pair.Value;

            if (row == null || _vehiclePrice.Next(pair.Key, row._Price_k__BackingField) is not { } price) continue;

            row._Price_k__BackingField = price;
            Reported.Scaled(_vehiclePrice, pair.Key, nameof(Config_Vehicle), pair.Key, nameof(Config_Vehicle.Price));
            _log($"vehicle {pair.Key} now costs {price} (game: {_vehiclePrice.Original(pair.Key)})");
        }
    }

    private readonly record struct Amounts(int TripCut, int CookingBonus, int LevelsSooner, int Affinity, double VehicleDiscount, int Exposure, double TravelCut, double DroneCut);
}
